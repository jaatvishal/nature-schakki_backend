using Core;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class OrderService(
    StoreContext context,
    IInventoryService inventoryService,
    ICouponService couponService,
    INotificationService notificationService,
    IOrderNotificationService orderNotificationService) : IOrderService
{
    public async Task<Order> CreateOrderAsync(
        int userId, string buyerEmail, Address shipToAddress,
        int deliveryMethodId, List<CartItem> items, string? couponCode = null)
    {
        if (items.Count == 0)
            throw new BadRequestException("Order must contain at least one item.");

        var deliveryMethod = await context.DeliveryMethods.FindAsync(deliveryMethodId)
            ?? throw new NotFoundException("Delivery method not found.");

        var productIds = items.Select(x => x.ProductId).ToList();
        var products = await context.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

        if (products.Count != productIds.Distinct().Count())
            throw new BadRequestException("One or more products not found.");

        foreach (var item in items)
        {
            if (!await inventoryService.HasAvailableStockAsync(item.ProductId, item.Quantity))
                throw new BadRequestException($"Insufficient stock for product {item.ProductId}.");
        }

        var orderItems = items.Select(item =>
        {
            var product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                PictureUrl = product.PictureUrl,
                Price = product.Price,
                Quantity = item.Quantity
            };
        }).ToList();

        var subtotal = orderItems.Sum(x => x.Price * x.Quantity);
        var deliveryCost = deliveryMethod.Price;
        decimal discount = 0;
        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            coupon = await couponService.ValidateCouponAsync(couponCode, userId, subtotal);
            discount = couponService.CalculateDiscount(coupon, subtotal);
        }

        var order = new Order
        {
            UserId = userId,
            BuyerEmail = buyerEmail,
            ShipToAddress = shipToAddress,
            DeliveryMethodId = deliveryMethodId,
            OrderItems = orderItems,
            Subtotal = subtotal,
            DeliveryCost = deliveryCost,
            Discount = discount,
            Total = subtotal + deliveryCost - discount,
            Status = OrderStatus.Pending,
            CouponId = coupon?.Id
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        foreach (var item in items)
            await inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);

        if (coupon != null)
            await couponService.RecordUsageAsync(coupon.Id, userId, order.Id);

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId, int userId)
    {
        return await context.Orders
            .Include(x => x.OrderItems)
            .Include(x => x.DeliveryMethod)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(int userId)
    {
        return await context.Orders
            .AsNoTracking()
            .Include(x => x.OrderItems)
            .Include(x => x.DeliveryMethod)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync();
    }

    public async Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        var order = await context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        if (order.Status == status)
            return order;

        if (!OrderStatusRules.CanTransition(order.Status, status))
            throw new BadRequestException($"Cannot transition order from {order.Status} to {status}.");

        var previousStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
        {
            foreach (var item in order.OrderItems)
                await inventoryService.ReleaseStockAsync(item.ProductId, item.Quantity);
        }

        if (OrderStatusRules.ConfirmsPayment(previousStatus, status))
        {
            foreach (var item in order.OrderItems)
            {
                await inventoryService.AdjustStockAsync(item.ProductId, -item.Quantity);
                var inventory = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == item.ProductId);
                if (inventory != null)
                    inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - item.Quantity);
            }
        }

        await context.SaveChangesAsync();

        var title = OrderStatusRules.NotificationTitle(status);
        var message = OrderStatusRules.NotificationMessage(status, order.Id);
        await notificationService.CreateAsync(order.UserId, title, message, $"/account/orders/{order.Id}");
        await orderNotificationService.NotifyOrderStatusChangedAsync(order.UserId, order.Id, status, message);

        return order;
    }

    public async Task CancelOrderAsync(int orderId, int userId)
    {
        var order = await context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        if (order.Status is OrderStatus.Shipped or OrderStatus.OutForDelivery or OrderStatus.Delivered)
            throw new BadRequestException("Cannot cancel a shipped or delivered order.");

        await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
    }
}
