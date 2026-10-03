using System.Security.Cryptography;
using System.Text;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class PasswordResetService(
    AppIdentityDbContext context,
    UserManager<AppUser> userManager,
    IEmailService emailService,
    IOptions<PasswordResetOptions> options,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    public async Task RequestAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null || !user.EmailConfirmed || !user.IsActive)
        {
            logger.LogInformation("Password reset requested for an account that cannot be reset.");
            return;
        }

        var now = DateTime.UtcNow;
        var activeTokens = await context.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var active in activeTokens)
            active.RevokedAtUtc = now;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = now.AddMinutes(options.Value.ExpiryMinutes)
        });
        await context.SaveChangesAsync(cancellationToken);

        var clientUrl = configuration["ClientUrl"] ?? "http://localhost:4200";
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing") &&
            !clientUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Password reset email was not sent because ClientUrl is not HTTPS.");
            return;
        }

        var resetUrl = $"{clientUrl.TrimEnd('/')}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";
        try
        {
            await emailService.SendEmailAsync(
                user.Email!,
                "Reset your Nature's Chakki password",
                $"Use this link to reset your password. It expires in {options.Value.ExpiryMinutes} minutes: {resetUrl}",
                cancellationToken);
            logger.LogInformation("Password reset email generated for user {UserId}.", user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password reset email delivery failed for user {UserId}.", user.Id);
        }
    }

    public async Task ResetAsync(string rawToken, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new BadRequestException("This password reset link is invalid or has expired.");

        var hash = Hash(rawToken);
        var record = await context.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        var now = DateTime.UtcNow;
        if (record == null || record.UsedAtUtc != null || record.RevokedAtUtc != null || record.ExpiresAtUtc <= now)
            throw new BadRequestException("This password reset link is invalid or has expired.");

        var user = await userManager.FindByIdAsync(record.UserId.ToString());
        if (user == null || !user.IsActive)
            throw new BadRequestException("This password reset link is invalid or has expired.");

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, identityToken, newPassword);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(x => x.Description)));

        record.UsedAtUtc = now;
        var others = await context.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.Id != record.Id && x.UsedAtUtc == null && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
            other.RevokedAtUtc = now;
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset completed for user {UserId}.", user.Id);
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
