using Core.Enums;

namespace Core.Entities;

public class Coupon : BaseEntity
{
    public required string Code { get; set; }
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CouponUsage> Usages { get; set; } = [];
}
