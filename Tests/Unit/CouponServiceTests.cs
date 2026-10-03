using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Unit;

public class CouponServiceTests
{
    private static StoreContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StoreContext(options);
    }

    [Fact]
    public void CalculateDiscount_Percentage_ReturnsCorrectAmount()
    {
        using var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon { Code = "SAVE10", Type = CouponType.Percentage, Value = 10 };

        var discount = service.CalculateDiscount(coupon, 100m);

        Assert.Equal(10m, discount);
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_CapsAtOrderTotal()
    {
        using var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon { Code = "FLAT50", Type = CouponType.FixedAmount, Value = 50 };

        var discount = service.CalculateDiscount(coupon, 30m);

        Assert.Equal(30m, discount);
    }

    [Fact]
    public void CalculateDiscount_RespectsMinimumOrderAmount()
    {
        using var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon { Code = "MIN100", Type = CouponType.Percentage, Value = 10, MinimumOrderAmount = 100 };

        var discount = service.CalculateDiscount(coupon, 50m);

        Assert.Equal(0m, discount);
    }

    [Fact]
    public async Task ValidateCouponAsync_ThrowsWhenExpired()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "EXPIRED",
            Type = CouponType.Percentage,
            Value = 10,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var service = new CouponService(context);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ValidateCouponAsync("EXPIRED", 1, 100m));
    }
}
