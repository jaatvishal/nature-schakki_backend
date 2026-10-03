using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Data;

public static class IdentitySeed
{
    public static async Task SeedUsersAsync(IServiceProvider services, IHostEnvironment env)
    {
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        var roles = new[] { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        if (env.IsDevelopment())
        {
            if (!config.GetValue<bool>("SeedUsers:Enabled")) return;
            await CreateUserAsync(
                userManager,
                Required("SeedUsers:AdminEmail"),
                "Admin User",
                Required("SeedUsers:AdminPassword"),
                "Admin");
            await CreateUserAsync(
                userManager,
                Required("SeedUsers:CustomerEmail"),
                "Test Customer",
                Required("SeedUsers:CustomerPassword"),
                "Customer");
            return;
        }

        if (config.GetValue<bool>("SeedUsers:Enabled"))
        {
            await CreateUserAsync(
                userManager,
                Required("SeedUsers:AdminEmail"),
                "Admin User",
                Required("SeedUsers:AdminPassword"),
                "Admin");
        }

        string Required(string key) =>
            config[key] ?? throw new InvalidOperationException($"{key} must be configured.");
    }

    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager, string email, string displayName, string password, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            if (!await userManager.CheckPasswordAsync(existing, password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                await userManager.ResetPasswordAsync(existing, token, password);
            }
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);
            return;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
