using API.Hubs;
using Core;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class OrderNotificationService(IHubContext<OrderHub> hubContext) : IOrderNotificationService
{
    public async Task NotifyOrderStatusChangedAsync(int userId, int orderId, OrderStatus status, string? message = null)
    {
        var payload = new
        {
            orderId,
            status = status.ToString(),
            displayStatus = OrderStatusRules.DisplayName(status),
            message = message ?? OrderStatusRules.NotificationMessage(status, orderId)
        };

        await hubContext.Clients.Group($"order-{orderId}").SendAsync("OrderStatusChanged", payload);
        await hubContext.Clients.Group($"user-{userId}").SendAsync("OrderStatusChanged", payload);
    }
}
