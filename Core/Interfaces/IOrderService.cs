using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int userId, string buyerEmail, Address shipToAddress, int deliveryMethodId, List<CartItem> items, string? couponCode = null);
    Task<Order?> GetOrderByIdAsync(int orderId, int userId);
    Task<IReadOnlyList<Order>> GetOrdersForUserAsync(int userId);
    Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task CancelOrderAsync(int orderId, int userId);
}
