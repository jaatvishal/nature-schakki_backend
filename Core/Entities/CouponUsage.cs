namespace Core.Entities;

public class CouponUsage : BaseEntity
{
    public int CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
