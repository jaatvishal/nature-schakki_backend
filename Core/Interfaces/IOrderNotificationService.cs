using Core.Enums;

namespace Core.Interfaces;

public interface IOrderNotificationService
{
    Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus status, string? message = null);
}
