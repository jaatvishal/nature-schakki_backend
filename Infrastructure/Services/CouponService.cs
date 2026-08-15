using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CouponService(StoreContext context) : ICouponService
{
    public decimal CalculateDiscount(Coupon coupon, decimal orderTotal)
    {
        if (coupon.MinimumOrderAmount.HasValue && orderTotal < coupon.MinimumOrderAmount.Value)
            return 0;

        return coupon.Type switch
        {
            CouponType.Percentage => Math.Round(orderTotal * coupon.Value / 100m, 2),
            CouponType.FixedAmount => Math.Min(coupon.Value, orderTotal),
            _ => 0
        };
    }

    public async Task<Coupon?> ValidateCouponAsync(string code, int userId, decimal orderTotal)
    {
        var coupon = await context.Coupons
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);

        if (coupon == null)
            throw new NotFoundException($"Coupon '{code}' not found.");

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Coupon has expired.");

        if (coupon.MaxUsageCount.HasValue && coupon.UsageCount >= coupon.MaxUsageCount)
            throw new BadRequestException("Coupon usage limit reached.");

        if (coupon.MinimumOrderAmount.HasValue && orderTotal < coupon.MinimumOrderAmount.Value)
            throw new BadRequestException($"Minimum order amount of {coupon.MinimumOrderAmount:C} required.");

        return coupon;
    }

    public async Task RecordUsageAsync(int couponId, int userId, int orderId)
    {
        var coupon = await context.Coupons.FindAsync(couponId)
            ?? throw new NotFoundException("Coupon not found.");

        coupon.UsageCount++;
        context.CouponUsages.Add(new CouponUsage
        {
            CouponId = couponId,
            UserId = userId,
            OrderId = orderId
        });
        await context.SaveChangesAsync();
    }
}
