using Core.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Unit;

public class InventoryServiceTests
{
    private static (StoreContext context, InventoryService service) CreateFixture()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new StoreContext(options);
        return (context, new InventoryService(context));
    }

    [Fact]
    public async Task ReserveStockAsync_SucceedsWhenStockAvailable()
    {
        var (context, service) = CreateFixture();
        context.Products.Add(new Product
        {
            Id = 1, Name = "P", Description = "D", Price = 1, PictureUrl = "/p", Type = "T", Brand = "B",
            QuantityInStock = 10, CategoryId = 1, BrandId = 1
        });
        context.Inventories.Add(new Inventory { ProductId = 1, QuantityOnHand = 10 });
        await context.SaveChangesAsync();

        var result = await service.ReserveStockAsync(1, 3);

        Assert.True(result);
        var inventory = await context.Inventories.FirstAsync();
        Assert.Equal(3, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task ReserveStockAsync_FailsWhenInsufficientStock()
    {
        var (context, service) = CreateFixture();
        context.Products.Add(new Product
        {
            Id = 1, Name = "P", Description = "D", Price = 1, PictureUrl = "/p", Type = "T", Brand = "B",
            QuantityInStock = 2, CategoryId = 1, BrandId = 1
        });
        context.Inventories.Add(new Inventory { ProductId = 1, QuantityOnHand = 2, ReservedQuantity = 1 });
        await context.SaveChangesAsync();

        var result = await service.ReserveStockAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task AdjustStockAsync_UpdatesProductQuantity()
    {
        var (context, service) = CreateFixture();
        context.Products.Add(new Product
        {
            Id = 1, Name = "P", Description = "D", Price = 1, PictureUrl = "/p", Type = "T", Brand = "B",
            QuantityInStock = 10, CategoryId = 1, BrandId = 1
        });
        context.Inventories.Add(new Inventory { ProductId = 1, QuantityOnHand = 10 });
        await context.SaveChangesAsync();

        await service.AdjustStockAsync(1, -3);

        var product = await context.Products.FindAsync(1);
        Assert.Equal(7, product!.QuantityInStock);
    }
}
