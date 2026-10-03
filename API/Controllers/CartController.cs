using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CartController(ICartWorkflow carts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ShoppingCart>> GetCartById(string id, CancellationToken cancellationToken) =>
        Ok(await carts.GetAsync(id, CurrentUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ShoppingCart>> UpdateCart(ShoppingCart cart, CancellationToken cancellationToken) =>
        Ok(await carts.ReplaceAsync(cart, CurrentUserId(), cancellationToken));

    [Authorize]
    [HttpPost("items")]
    public async Task<ActionResult<ShoppingCart>> ChangeItem(CartItemChangeDto dto, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        return Ok(await carts.ChangeItemAsync(dto.ProductId, dto.QuantityKg, dto.Mode, userId.Value, cancellationToken));
    }

    [Authorize]
    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<ShoppingCart>> RemoveItem(int productId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        return Ok(await carts.RemoveItemAsync(productId, userId.Value, cancellationToken));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCart(string id, CancellationToken cancellationToken)
    {
        await carts.DeleteAsync(id, CurrentUserId(), cancellationToken);
        return NoContent();
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
