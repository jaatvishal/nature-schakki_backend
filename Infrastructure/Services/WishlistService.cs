using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class WishlistService(StoreContext context) : IWishlistService
{
    public async Task<Wishlist> GetOrCreateWishlistAsync(int userId)
    {
        var wishlist = await context.Wishlists
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wishlist != null) return wishlist;

        wishlist = new Wishlist { UserId = userId };
        context.Wishlists.Add(wishlist);
        await context.SaveChangesAsync();
        return wishlist;
    }

    public async Task<Wishlist> AddItemAsync(int userId, int productId)
    {
        var wishlist = await GetOrCreateWishlistAsync(userId);
        if (wishlist.Items.Any(x => x.ProductId == productId))
            return wishlist;

        wishlist.Items.Add(new WishlistItem { ProductId = productId, WishlistId = wishlist.Id });
        await context.SaveChangesAsync();
        return await GetOrCreateWishlistAsync(userId);
    }

    public async Task RemoveItemAsync(int userId, int productId)
    {
        var wishlist = await context.Wishlists
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wishlist == null) return;

        var item = wishlist.Items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            context.WishlistItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }
}
