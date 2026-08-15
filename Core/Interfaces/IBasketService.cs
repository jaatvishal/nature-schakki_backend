using Core.Entities;

namespace Core.Interfaces;

public interface IBasketService
{
    Task<ShoppingCart?> GetBasketAsync(string basketId);
    Task<ShoppingCart> UpdateBasketAsync(ShoppingCart basket);
    Task DeleteBasketAsync(string basketId);
}
