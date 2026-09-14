using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IEmailService emailService,
    IEmailVerificationService emailVerificationService,
    AppIdentityDbContext identityContext,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResultDto>> Register(
        RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return Conflict(existingUser.EmailConfirmed
                ? "Email is already registered."
                : "Registration is pending email verification. Request a new code.");
        }

        var displayName = !string.IsNullOrWhiteSpace(dto.DisplayName)
            ? dto.DisplayName
            : $"{dto.FirstName} {dto.LastName}".Trim();

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = displayName,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.GroupBy(x => x.Code)
                    .ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray())));

        var roleResult = await userManager.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Problem(statusCode: 500, title: "Registration failed.");
        }

        await emailVerificationService.SendOtpAsync(
            user.Id, user.Email!, false, cancellationToken);

        return Accepted(new RegisterResultDto
        {
            Email = user.Email!,
            Message = "Registration successful. Check your email for the verification code."
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> VerifyEmail(
        VerifyEmailOtpDto dto,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return BadRequest("Verification code is invalid or expired.");
        if (user.EmailConfirmed)
            return Conflict("Email is already verified.");

        await emailVerificationService.VerifyOtpAsync(user.Id, dto.Otp, cancellationToken);
        return Ok(await CreateUserDto(user));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResultDto>> ResendVerification(
        ResendEmailOtpDto dto,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Ok(new RegisterResultDto
            {
                Email = dto.Email,
                Message = "If the account is awaiting verification, a new code has been sent."
            });
        if (user.EmailConfirmed)
            return Conflict("Email is already verified.");

        await emailVerificationService.SendOtpAsync(
            user.Id, user.Email!, true, cancellationToken);
        return Ok(new RegisterResultDto
        {
            Email = user.Email!,
            Message = "A new verification code has been sent."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized("Invalid credentials.");

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded) return Unauthorized("Invalid credentials.");
        if (!user.EmailConfirmed) return Unauthorized("Email verification is required.");

        return await CreateUserDto(user);
    }

    [Authorize]
    [HttpGet("profile")]
    public Task<ActionResult<UserDto>> GetProfile() => GetCurrentUser();

    [Authorize]
    [HttpGet("current")]
    [HttpGet("current-user")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (email == null) return Unauthorized();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) return Unauthorized();
        return await CreateUserDto(user, includeNewTokens: false);
    }

    [HttpPost("refresh")]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<UserDto>> RefreshToken([FromBody] RefreshTokenDto? dto)
    {
        var token = dto?.RefreshToken ?? Request.Headers["X-Refresh-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return Unauthorized("Invalid refresh token.");

        var refreshToken = await tokenService.ValidateRefreshTokenAsync(token);
        if (refreshToken == null) return Unauthorized("Invalid refresh token.");

        var user = await userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user == null) return Unauthorized();

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        await identityContext.SaveChangesAsync();

        return await CreateUserDto(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tokens = await identityContext.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await identityContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Ok(new { message = "If the email exists, a reset link was sent." });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetUrl = $"{config["ClientUrl"] ?? "http://localhost:4200"}/auth/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={encoded}";

        await emailService.SendEmailAsync(user.Email!,
            "Reset your password",
            $"Reset your password: {resetUrl}");

        return Ok(new { message = "If the email exists, a reset link was sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return BadRequest("Invalid request.");

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
        var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new { message = "Password reset successful." });
    }

    private async Task<UserDto> CreateUserDto(AppUser user, bool includeNewTokens = true)
    {
        var roles = await userManager.GetRolesAsync(user);
        var parts = user.DisplayName.Split(' ', 2);
        var dto = new UserDto
        {
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            FirstName = parts.Length > 0 ? parts[0] : user.DisplayName,
            LastName = parts.Length > 1 ? parts[1] : string.Empty,
            Roles = roles.ToList()
        };

        if (!includeNewTokens) return dto;

        dto.Token = tokenService.CreateToken(user.Id, user.Email!, user.DisplayName, roles);
        dto.RefreshToken = tokenService.GenerateRefreshToken();

        identityContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = dto.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await identityContext.SaveChangesAsync();

        return dto;
    }
}
