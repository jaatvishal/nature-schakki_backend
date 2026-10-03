using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(50);
        builder.Property(x => x.Unit).HasMaxLength(30);
        builder.HasIndex(x => x.Sku).IsUnique().HasFilter("[Sku] <> ''");
        builder.HasIndex(x => new { x.IsArchived, x.IsActive });
        builder.Property(x => x.Brand).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductBrand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
