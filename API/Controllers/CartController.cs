using Core.Entities;
using Core.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace API.Controllers
{
    [Route("api/[controller]")]
    //[ApiController]
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
            if (updatedCart == null) return BadRequest("Problem updating the cart");
            return Ok(updatedCart);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteCart(string id)
        {
            var deleted = await cartService.DeleteCartAsync(id);
            if (!deleted) return BadRequest("Problem deleting the cart");
            return Ok();

        }
    }
}
