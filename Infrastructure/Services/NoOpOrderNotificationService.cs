using Core.Enums;
using Core.Interfaces;

namespace Infrastructure.Services;

public class NoOpOrderNotificationService : IOrderNotificationService
{
    public Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus status, string? message = null) =>
        Task.CompletedTask;
}
