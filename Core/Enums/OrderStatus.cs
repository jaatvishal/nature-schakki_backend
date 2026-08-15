namespace Core.Enums;

public enum OrderStatus
{
    Pending,
    PaymentReceived, // displayed as "Paid" in UI
    Processing,
    Packed,
    Shipped,
    OutForDelivery,
    Delivered,
    Cancelled,
    Refunded,
    Failed
}
