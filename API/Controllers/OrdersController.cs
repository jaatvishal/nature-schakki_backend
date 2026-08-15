using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class OrdersController(
    IOrderService orderService,
    ICartService cartService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var cartId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart = await cartService.GetCartAsync(cartId);

        if (cart == null || cart.Items.Count == 0)
            return BadRequest("Cart is empty.");

        var address = new Address
        {
            FirstName = dto.ShipToAddress.FirstName,
            LastName = dto.ShipToAddress.LastName,
            Street = dto.ShipToAddress.Street,
            City = dto.ShipToAddress.City,
            State = dto.ShipToAddress.State,
            ZipCode = dto.ShipToAddress.ZipCode,
            Country = dto.ShipToAddress.Country,
            UserId = userId
        };

        var order = await orderService.CreateOrderAsync(
            userId, email, address, dto.DeliveryMethodId, cart.Items, dto.CouponCode);

        if (dto.PaymentMethod.Equals("cod", StringComparison.OrdinalIgnoreCase))
            await orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

        await cartService.DeleteCartAsync(cartId);
        return Ok(await orderService.GetOrderByIdAsync(order.Id, userId) ?? order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Order>>> GetOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await orderService.GetOrdersForUserAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await orderService.GetOrderByIdAsync(id, userId);
        return order == null ? NotFound() : order;
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await orderService.CancelOrderAsync(id, userId);
        return NoContent();
    }
}
