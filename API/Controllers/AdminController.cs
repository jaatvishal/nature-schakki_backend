using Asp.Versioning;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admin")]
[ApiController]
public class AdminController(
    StoreContext context,
    UserManager<AppUser> userManager,
    IOrderService orderService,
    IReviewService reviewService) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts() =>
        Ok(await context.Products.AsNoTracking().ToListAsync());

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id) return BadRequest();
        context.Products.Update(product);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null) return NotFound();
        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders() =>
        Ok(await context.Orders.AsNoTracking().Include(x => x.OrderItems).ToListAsync());

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
    {
        var order = await orderService.UpdateOrderStatusAsync(id, status);
        return Ok(order);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync();
        return Ok(users.Select(u => new { u.Id, u.Email, u.DisplayName }));
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> GetCoupons() =>
        Ok(await context.Coupons.AsNoTracking().ToListAsync());

    [HttpPost("coupons")]
    public async Task<IActionResult> CreateCoupon(Coupon coupon)
    {
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        return Ok(coupon);
    }

    [HttpGet("reviews/pending")]
    public async Task<IActionResult> GetPendingReviews() =>
        Ok(await context.ProductReviews.AsNoTracking()
            .Where(x => x.Status == ReviewStatus.Pending).ToListAsync());

    [HttpPut("reviews/{id:int}/moderate")]
    public async Task<IActionResult> ModerateReview(int id, [FromBody] ReviewStatus status)
    {
        var review = await reviewService.ModerateReviewAsync(id, status);
        return Ok(review);
    }
}
