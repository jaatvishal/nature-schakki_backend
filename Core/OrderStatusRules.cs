using Core.Enums;

namespace Core;

public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.PaymentReceived, OrderStatus.Processing, OrderStatus.Failed, OrderStatus.Cancelled],
        [OrderStatus.PaymentReceived] = [OrderStatus.Processing, OrderStatus.Refunded, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Packed, OrderStatus.Cancelled],
        [OrderStatus.Packed] = [OrderStatus.Shipped],
        [OrderStatus.Shipped] = [OrderStatus.OutForDelivery],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
        [OrderStatus.Failed] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = [],
        [OrderStatus.Delivered] = []
    };

    public static readonly OrderStatus[] Timeline =
    [
        OrderStatus.Pending,
        OrderStatus.PaymentReceived,
        OrderStatus.Processing,
        OrderStatus.Packed,
        OrderStatus.Shipped,
        OrderStatus.OutForDelivery,
        OrderStatus.Delivered
    ];

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        from == to || Allowed.GetValueOrDefault(from)?.Contains(to) == true;

    public static bool ConfirmsPayment(OrderStatus from, OrderStatus to) =>
        from == OrderStatus.Pending &&
        (to == OrderStatus.PaymentReceived || to == OrderStatus.Processing);

    public static string DisplayName(OrderStatus status) =>
        status == OrderStatus.PaymentReceived ? "Paid" : status.ToString();

    public static string NotificationTitle(OrderStatus status) => status switch
    {
        OrderStatus.PaymentReceived => "Payment Successful",
        OrderStatus.Processing => "Order Processing",
        OrderStatus.Packed => "Order Packed",
        OrderStatus.Shipped => "Order Shipped",
        OrderStatus.OutForDelivery => "Out for Delivery",
        OrderStatus.Delivered => "Order Delivered",
        OrderStatus.Cancelled => "Order Cancelled",
        OrderStatus.Refunded => "Order Refunded",
        OrderStatus.Failed => "Payment Failed",
        _ => "Order Updated"
    };

    public static string NotificationMessage(OrderStatus status, int orderId) =>
        $"{NotificationTitle(status)} for order #{orderId}.";
}
