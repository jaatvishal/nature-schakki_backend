using Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _database.StringGetAsync(key);
        return data.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(data!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        await _database.StringSetAsync(key, JsonSerializer.Serialize(value), expiry);
    }

    public async Task RemoveAsync(string key) => await _database.KeyDeleteAsync(key);
}
