using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Services;

public class BasketService(ICartService cartService) : IBasketService
{
    public Task<ShoppingCart?> GetBasketAsync(string basketId) => cartService.GetCartAsync(basketId);

    public async Task<ShoppingCart> UpdateBasketAsync(ShoppingCart basket)
    {
        var result = await cartService.SetCartAsync(basket);
        return result ?? basket;
    }

    public async Task DeleteBasketAsync(string basketId) => await cartService.DeleteCartAsync(basketId);
}
