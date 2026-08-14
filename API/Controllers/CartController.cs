using Asp.Versioning;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ShoppingCart>> GetCartById(string id)
    {
        var cart = await cartService.GetCartAsync(id);
        return Ok(cart ?? new ShoppingCart { Id = id });
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingCart>> UpdateCart(ShoppingCart cart)
    {
        var updatedCart = await cartService.SetCartAsync(cart);
        return updatedCart == null ? BadRequest("Problem updating the cart") : Ok(updatedCart);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCart(string id)
    {
        var deleted = await cartService.DeleteCartAsync(id);
        return deleted ? Ok() : BadRequest("Problem deleting the cart");
    }
}
