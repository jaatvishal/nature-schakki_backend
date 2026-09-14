using API.Hubs;
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
    ICartService cartService,
    IOrderNotificationService notificationService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
    {
        if (!dto.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only Cash on Delivery is currently supported.");

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

        await cartService.DeleteCartAsync(cartId);
        return Ok(MapOrder(order));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await orderService.GetOrdersForUserAsync(userId);
        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await orderService.GetOrderByIdAsync(id, userId);
        return order == null ? NotFound() : Ok(MapOrder(order));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await orderService.CancelOrderAsync(id, userId);
        await notificationService.NotifyOrderStatusChangedAsync(id, OrderStatus.Cancelled);
        return NoContent();
    }

    private static OrderDto MapOrder(Order order) => new()
    {
        Id = order.Id,
        OrderDate = order.OrderDate,
        Status = order.Status.ToString(),
        PaymentMethod = order.PaymentMethod,
        PaymentStatus = "Pending",
        ShippingAddress = new AddressDto
        {
            FirstName = order.ShipToAddress.FirstName,
            LastName = order.ShipToAddress.LastName,
            Street = order.ShipToAddress.Street,
            City = order.ShipToAddress.City,
            State = order.ShipToAddress.State,
            ZipCode = order.ShipToAddress.ZipCode,
            Country = order.ShipToAddress.Country
        },
        DeliveryMethod = order.DeliveryMethod?.ShortName ?? "Standard Delivery",
        Subtotal = order.Subtotal,
        DeliveryCost = order.DeliveryCost,
        Discount = order.Discount,
        Total = order.Total,
        OrderItems = order.OrderItems.Select(x => new OrderItemDto
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            PictureUrl = x.PictureUrl,
            Price = x.Price,
            Quantity = x.Quantity
        }).ToList()
    };
}
