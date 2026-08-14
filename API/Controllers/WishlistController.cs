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
public class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await wishlistService.GetOrCreateWishlistAsync(userId));
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> AddItem(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await wishlistService.AddItemAsync(userId, productId));
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await wishlistService.RemoveItemAsync(userId, productId);
        return NoContent();
    }
}
