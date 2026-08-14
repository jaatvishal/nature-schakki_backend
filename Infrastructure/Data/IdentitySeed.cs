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
        if (!env.IsDevelopment()) return;

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        var roles = new[] { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        var adminEmail = config["SeedUsers:AdminEmail"] ?? "admin@natureschakki.com";
        var adminPassword = config["SeedUsers:AdminPassword"] ?? "Admin@123!";
        var customerEmail = config["SeedUsers:CustomerEmail"] ?? "customer@natureschakki.com";
        var customerPassword = config["SeedUsers:CustomerPassword"] ?? "Customer@123!";

        await CreateUserAsync(userManager, adminEmail, "Admin User", adminPassword, "Admin");
        await CreateUserAsync(userManager, customerEmail, "Test Customer", customerPassword, "Customer");
    }

    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager, string email, string displayName, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) != null) return;

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
