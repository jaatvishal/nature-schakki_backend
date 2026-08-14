using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<AppUser, AppRole, int>(options)
{
    public DbSet<Core.Entities.RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Core.Entities.RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
        });
    }
}
