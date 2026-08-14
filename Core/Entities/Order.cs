using Core.Enums;

namespace Core.Entities;

public class Order : BaseEntity
{
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public required string BuyerEmail { get; set; }
    public required Address ShipToAddress { get; set; }
    public int DeliveryMethodId { get; set; }
    public DeliveryMethod? DeliveryMethod { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal DeliveryCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? PaymentIntentId { get; set; }
    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
}
