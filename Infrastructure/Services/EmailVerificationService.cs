using System.Globalization;
using System.Security.Cryptography;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EmailVerificationService(
    AppIdentityDbContext context,
    UserManager<AppUser> userManager,
    IEmailService emailService,
    IOptions<EmailVerificationOptions> options) : IEmailVerificationService
{
    private readonly PasswordHasher<EmailVerificationOtp> _hasher = new();

    public async Task SendOtpAsync(
        int userId,
        string email,
        bool enforceCooldown,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var now = DateTime.UtcNow;
        var record = await context.EmailVerificationOtps
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (enforceCooldown && record != null &&
            record.LastSentAt.AddSeconds(settings.ResendCooldownSeconds) > now)
        {
            throw new TooManyRequestsException(
                $"Please wait {settings.ResendCooldownSeconds} seconds before requesting another code.");
        }

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString(CultureInfo.InvariantCulture);

        if (record == null)
        {
            record = new EmailVerificationOtp
            {
                UserId = userId,
                OtpHash = string.Empty,
                ExpiresAt = now,
                LastSentAt = now
            };
            context.EmailVerificationOtps.Add(record);
        }

        record.OtpHash = _hasher.HashPassword(record, otp);
        record.ExpiresAt = now.AddMinutes(settings.OtpLifetimeMinutes);
        record.LastSentAt = now;
        record.FailedAttempts = 0;
        record.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendEmailAsync(
                email,
                "Verify your Nature's Chakki account",
                $"Your verification code is {otp}. It expires in {settings.OtpLifetimeMinutes} minutes.",
                cancellationToken);
        }
        catch
        {
            record.LastSentAt = now.AddSeconds(-settings.ResendCooldownSeconds);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task VerifyOtpAsync(
        int userId,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var record = await context.EmailVerificationOtps
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new BadRequestException("Verification code is invalid or expired.");

        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            context.EmailVerificationOtps.Remove(record);
            await context.SaveChangesAsync(cancellationToken);
            throw new BadRequestException("Verification code is invalid or expired.");
        }

        if (record.FailedAttempts >= settings.MaxAttempts)
            throw new TooManyRequestsException("Maximum verification attempts exceeded. Request a new code.");

        var result = _hasher.VerifyHashedPassword(record, record.OtpHash, otp);
        if (result == PasswordVerificationResult.Failed)
        {
            record.FailedAttempts++;
            await context.SaveChangesAsync(cancellationToken);

            if (record.FailedAttempts >= settings.MaxAttempts)
                throw new TooManyRequestsException("Maximum verification attempts exceeded. Request a new code.");

            throw new BadRequestException("Verification code is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture))
            ?? throw new BadRequestException("Unable to verify account.");
        user.EmailConfirmed = true;
        var confirmed = await userManager.UpdateAsync(user);
        if (!confirmed.Succeeded)
            throw new BadRequestException("Unable to verify account.");

        context.EmailVerificationOtps.Remove(record);
        await context.SaveChangesAsync(cancellationToken);
    }
}
