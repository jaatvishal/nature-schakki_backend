using Core.Entities;
using Core.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Infrastructure.Services;

public class InMemoryCartStorage : ICartService
{
    private static readonly ConcurrentDictionary<string, string> Carts = new();

    public Task<bool> DeleteCartAsync(string key)
    {
        Carts.TryRemove(key, out _);
        return Task.FromResult(true);
    }

    public Task<ShoppingCart?> GetCartAsync(string key)
    {
        if (!Carts.TryGetValue(key, out var data))
            return Task.FromResult<ShoppingCart?>(null);
        return Task.FromResult(JsonSerializer.Deserialize<ShoppingCart>(data));
    }

    public async Task<ShoppingCart?> SetCartAsync(ShoppingCart cart)
    {
        Carts[cart.Id] = JsonSerializer.Serialize(cart);
        return await GetCartAsync(cart.Id);
    }
}
