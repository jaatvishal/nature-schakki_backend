using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<AppUser, AppRole, int>(options)
{
    public DbSet<Core.Entities.RefreshToken> RefreshTokens { get; set; }
    public DbSet<Core.Entities.EmailVerificationOtp> EmailVerificationOtps { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Core.Entities.RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
        });

        builder.Entity<Core.Entities.EmailVerificationOtp>(entity =>
        {
            entity.ToTable("EmailVerificationOtps");
            entity.Property(x => x.OtpHash).IsRequired().HasMaxLength(512);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne<AppUser>()
                .WithOne()
                .HasForeignKey<Core.Entities.EmailVerificationOtp>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
