using Core.Entities;
using Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Services;

public class RedisCartStorage(IConnectionMultiplexer redis) : ICartService
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<bool> DeleteCartAsync(string key) =>
        await _database.KeyDeleteAsync(key);

    public async Task<ShoppingCart?> GetCartAsync(string key)
    {
        var data = await _database.StringGetAsync(key);
        return data.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ShoppingCart>(data!);
    }

    public async Task<ShoppingCart?> SetCartAsync(ShoppingCart cart)
    {
        var created = await _database.StringSetAsync(cart.Id, JsonSerializer.Serialize(cart), TimeSpan.FromDays(30));
        return created ? await GetCartAsync(cart.Id) : null;
    }
}
