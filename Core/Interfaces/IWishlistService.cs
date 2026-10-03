using Core.Entities;

namespace Core.Interfaces;

public interface IWishlistService
{
    Task<Wishlist> GetOrCreateWishlistAsync(int userId);
    Task<Wishlist> AddItemAsync(int userId, int productId);
    Task RemoveItemAsync(int userId, int productId);
}
