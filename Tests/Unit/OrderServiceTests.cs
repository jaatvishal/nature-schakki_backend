using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Unit;

public class OrderServiceTests
{
    private static (StoreContext context, OrderService service) CreateFixture()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new StoreContext(options);
        var inventory = new InventoryService(context);
        var coupon = new CouponService(context);
        var notification = new NotificationService(context);
        var orderNotification = new NoOpOrderNotificationService();
        var orderService = new OrderService(context, inventory, coupon, notification, orderNotification);
        return (context, orderService);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_Cancelled_ReleasesReservedStock()
    {
        var (context, service) = CreateFixture();
        var product = new Product
        {
            Name = "Test",
            Description = "Test",
            Price = 10,
            PictureUrl = "/test.png",
            Type = "Flour",
            Brand = "Brand",
            QuantityInStock = 10,
            CategoryId = 1,
            BrandId = 1
        };
        context.Products.Add(product);
        context.ProductCategories.Add(new ProductCategory { Id = 1, Name = "Flour" });
        context.ProductBrands.Add(new ProductBrand { Id = 1, Name = "Brand" });
        context.DeliveryMethods.Add(new DeliveryMethod { Id = 1, ShortName = "STD", Description = "Standard", Price = 5, DeliveryTimeDays = 3 });
        await context.SaveChangesAsync();

        context.Inventories.Add(new Inventory { ProductId = product.Id, QuantityOnHand = 10, ReservedQuantity = 2 });
        await context.SaveChangesAsync();

        var order = new Order
        {
            UserId = 1,
            BuyerEmail = "test@test.com",
            ShipToAddress = new Address
            {
                FirstName = "A", LastName = "B", Street = "1 St", City = "C", State = "S", ZipCode = "1", Country = "US", UserId = 1
            },
            DeliveryMethodId = 1,
            Subtotal = 20,
            DeliveryCost = 5,
            Total = 25,
            Status = OrderStatus.Pending,
            OrderItems =
            [
                new OrderItem { ProductId = product.Id, ProductName = "Test", PictureUrl = "/t.png", Price = 10, Quantity = 2 }
            ]
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled);

        var inventory = await context.Inventories.FirstAsync(x => x.ProductId == product.Id);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(OrderStatus.Cancelled, (await context.Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_Throws()
    {
        var (context, service) = CreateFixture();
        context.DeliveryMethods.Add(new DeliveryMethod { Id = 1, ShortName = "STD", Description = "Standard", Price = 5, DeliveryTimeDays = 3 });
        var order = new Order
        {
            UserId = 1,
            BuyerEmail = "test@test.com",
            ShipToAddress = new Address
            {
                FirstName = "A", LastName = "B", Street = "1 St", City = "C", State = "S", ZipCode = "1", Country = "US", UserId = 1
            },
            DeliveryMethodId = 1,
            Subtotal = 20,
            DeliveryCost = 5,
            Total = 25,
            Status = OrderStatus.Pending,
            OrderItems = []
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered));
    }

    [Fact]
    public async Task CreateOrderAsync_ThrowsWhenCartEmpty()
    {
        var (_, service) = CreateFixture();

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateOrderAsync(
            1, "test@test.com",
            new Address { FirstName = "A", LastName = "B", Street = "1", City = "C", State = "S", ZipCode = "1", Country = "US", UserId = 1 },
            1, []));
    }
}
