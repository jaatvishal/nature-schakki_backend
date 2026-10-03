using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

public class StoreContextFactory : IDesignTimeDbContextFactory<StoreContext>
{
    public StoreContext CreateDbContext(string[] args)
    {
        var connectionString = GetConnectionString();
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new StoreContext(options);
    }

    private static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? throw new InvalidOperationException(
            "Set ConnectionStrings__DefaultConnection before running EF Core tools.");
}

public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection before running EF Core tools.");
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new AppIdentityDbContext(options);
    }
}
