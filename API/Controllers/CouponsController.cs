using Asp.Versioning;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CouponsController(ICouponService couponService) : ControllerBase
{
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var coupon = await couponService.ValidateCouponAsync(request.Code, userId, request.OrderTotal);
        var discount = couponService.CalculateDiscount(coupon!, request.OrderTotal);
        return Ok(new { coupon!.Code, discount, coupon.Type, coupon.Value });
    }
}

public record ValidateCouponRequest(string Code, decimal OrderTotal);
