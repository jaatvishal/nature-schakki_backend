using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public async Task JoinOrderGroup(int orderId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

    public async Task LeaveOrderGroup(int orderId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
}

public interface IOrderNotificationService
{
    Task NotifyOrderStatusChangedAsync(int orderId, OrderStatus status);
}

public class OrderNotificationService(IHubContext<OrderHub> hubContext) : IOrderNotificationService
{
    public async Task NotifyOrderStatusChangedAsync(int orderId, OrderStatus status) =>
        await hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("OrderStatusChanged", new { orderId, status = status.ToString() });
}
