using System.Text.Json;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context)
    {
        if (!await context.DeliveryMethods.AnyAsync())
        {
            var deliveryPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Data", "SeedData", "delivery.json");
            if (!File.Exists(deliveryPath))
                deliveryPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "SeedData", "delivery.json");

            if (File.Exists(deliveryPath))
            {
                var deliveryData = await File.ReadAllTextAsync(deliveryPath);
                var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(deliveryData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (methods != null)
                {
                    context.DeliveryMethods.AddRange(methods);
                    await context.SaveChangesAsync();
                }
            }
        }

        if (!await context.Products.AnyAsync())
        {
            var productsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Data", "SeedData", "products.json");
            if (!File.Exists(productsPath))
                productsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Data", "SeedData", "products.json");

            if (!File.Exists(productsPath))
            {
                productsPath = "../Infrastructure/Data/SeedData/products.json";
            }

            var productsData = await File.ReadAllTextAsync(productsPath);
            var seedProducts = JsonSerializer.Deserialize<List<ProductSeedDto>>(productsData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (seedProducts == null || seedProducts.Count == 0) return;

            var brandNames = seedProducts.Select(x => x.Brand).Distinct().ToList();
            var typeNames = seedProducts.Select(x => x.Type).Distinct().ToList();

            foreach (var brandName in brandNames)
            {
                if (!await context.ProductBrands.AnyAsync(x => x.Name == brandName))
                    context.ProductBrands.Add(new ProductBrand { Name = brandName });
            }

            foreach (var typeName in typeNames)
            {
                if (!await context.ProductCategories.AnyAsync(x => x.Name == typeName))
                    context.ProductCategories.Add(new ProductCategory { Name = typeName });
            }

            await context.SaveChangesAsync();

            var brands = await context.ProductBrands.ToDictionaryAsync(x => x.Name, x => x.Id);
            var categories = await context.ProductCategories.ToDictionaryAsync(x => x.Name, x => x.Id);

            foreach (var seed in seedProducts)
            {
                var product = new Product
                {
                    Name = seed.Name,
                    Description = seed.Description,
                    Price = seed.Price,
                    PictureUrl = seed.PictureUrl,
                    Type = seed.Type,
                    Brand = seed.Brand,
                    QuantityInStock = seed.QuantityInStock,
                    BrandId = brands[seed.Brand],
                    CategoryId = categories[seed.Type]
                };
                context.Products.Add(product);
            }

            await context.SaveChangesAsync();

            var products = await context.Products.ToListAsync();
            foreach (var product in products)
            {
                context.Inventories.Add(new Inventory
                {
                    ProductId = product.Id,
                    QuantityOnHand = product.QuantityInStock
                });
            }

            await context.SaveChangesAsync();
        }
    }

    private class ProductSeedDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public required string PictureUrl { get; set; }
        public required string Type { get; set; }
        public required string Brand { get; set; }
        public int QuantityInStock { get; set; }
    }
}
