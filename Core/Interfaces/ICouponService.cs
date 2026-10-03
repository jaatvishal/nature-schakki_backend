using Core.Entities;

namespace Core.Interfaces;

public interface ICouponService
{
    decimal CalculateDiscount(Coupon coupon, decimal orderTotal);
    Task<Coupon?> ValidateCouponAsync(string code, int userId, decimal orderTotal);
    Task RecordUsageAsync(int couponId, int userId, int orderId);
}
