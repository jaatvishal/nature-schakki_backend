using Asp.Versioning;
using Core.DTOs;
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

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}

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
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        const int lowStockThreshold = 10;

        var payments = await context.Payments.AsNoTracking().ToListAsync();
        var recentOrders = await context.Orders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Take(5)
            .Select(o => new { o.Id, o.BuyerEmail, o.Status, o.Total, o.OrderDate })
            .ToListAsync();

        var lowStock = await context.Inventories.AsNoTracking()
            .CountAsync(x => x.QuantityOnHand - x.ReservedQuantity <= lowStockThreshold);

        var activities = recentOrders.Select(o => new AdminActivityDto
        {
            Type = "Order",
            Description = $"Order #{o.Id} ({o.BuyerEmail}) — {o.Status} — ₹{o.Total:N2}",
            OccurredAt = o.OrderDate
        }).ToList();

        activities.AddRange(payments.OrderByDescending(p => p.CreatedAt).Take(5).Select(p => new AdminActivityDto
        {
            Type = "Payment",
            Description = $"Payment for order #{p.OrderId} — {p.Status} — ₹{p.Amount:N2}",
            OccurredAt = p.CreatedAt
        }));

        return Ok(new AdminDashboardDto
        {
            UserCount = await userManager.Users.CountAsync(),
            ProductCount = await context.Products.CountAsync(),
            OrderCount = await context.Orders.CountAsync(),
            Revenue = await context.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount),
            SuccessfulPayments = payments.Count(p => p.Status == PaymentStatus.Succeeded),
            FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed),
            PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending),
            LowStockCount = lowStock,
            RecentActivities = activities.OrderByDescending(a => a.OccurredAt).Take(8).ToList()
        });
    }

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
        Ok(await context.Orders.AsNoTracking()
            .Include(x => x.OrderItems)
            .Include(x => x.DeliveryMethod)
            .Select(o => new
            {
                o.Id,
                o.BuyerEmail,
                o.OrderDate,
                Status = o.Status.ToString(),
                o.Subtotal,
                o.DeliveryCost,
                o.Total,
                o.PaymentIntentId,
                PaymentStatus = context.Payments
                    .Where(p => p.OrderId == o.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Status.ToString())
                    .FirstOrDefault() ?? "Pending",
                OrderItems = o.OrderItems.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.PictureUrl,
                    i.Price,
                    i.Quantity
                })
            }).ToListAsync());

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments() =>
        Ok(await context.Payments.AsNoTracking()
            .Include(p => p.Order)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                BuyerEmail = p.Order!.BuyerEmail,
                p.Amount,
                Status = p.Status.ToString(),
                p.PaymentIntentId,
                p.CreatedAt
            }).ToListAsync());

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await orderService.UpdateOrderStatusAsync(id, dto.Status);
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
