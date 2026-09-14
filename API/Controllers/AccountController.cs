using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    AppIdentityDbContext identityContext,
    IEmailVerificationService emailVerificationService) : ControllerBase
{
    [HttpPost("register")]
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

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}",
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors
                    .GroupBy(x => x.Code)
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
    public async Task<ActionResult<UserDto>> Login(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized("Invalid credentials.");

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded) return Unauthorized("Invalid credentials.");
        if (!user.EmailConfirmed)
            return Unauthorized("Email verification is required.");

        return await CreateUserDto(user);
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await userManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email)!);
        if (user == null) return Unauthorized();
        return await CreateUserDto(user, issueRefreshToken: false);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<UserDto>> RefreshToken(RefreshTokenDto dto)
    {
        var refreshToken = await tokenService.ValidateRefreshTokenAsync(dto.RefreshToken);
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

    private async Task<UserDto> CreateUserDto(
        AppUser user,
        bool issueRefreshToken = true)
    {
        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user.Id, user.Email!, user.DisplayName, roles);
        var refreshToken = string.Empty;
        if (issueRefreshToken)
        {
            refreshToken = tokenService.GenerateRefreshToken();
            identityContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await identityContext.SaveChangesAsync();
        }

        var names = user.DisplayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new UserDto
        {
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            FirstName = names.ElementAtOrDefault(0) ?? user.DisplayName,
            LastName = names.ElementAtOrDefault(1) ?? string.Empty,
            Roles = roles.ToList(),
            Token = token,
            RefreshToken = refreshToken
        };
    }
}
