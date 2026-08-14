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
    AppIdentityDbContext identityContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
    {
        if (await userManager.FindByEmailAsync(dto.Email) != null)
            return BadRequest("Email is already registered.");

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, "Customer");

        return await CreateUserDto(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized("Invalid credentials.");

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded) return Unauthorized("Invalid credentials.");

        return await CreateUserDto(user);
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await userManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email)!);
        if (user == null) return Unauthorized();
        return await CreateUserDto(user);
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

    private async Task<UserDto> CreateUserDto(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user.Id, user.Email!, user.DisplayName, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        identityContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await identityContext.SaveChangesAsync();

        return new UserDto(user.Email!, user.DisplayName, token, refreshToken);
    }
}
