using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace IdealWeightNutrition.Infrastructure.Caching;

internal static class DistributedCacheJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<T?> GetAsync<T>(
        IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
    }

    public static Task SetAsync<T>(
        IDistributedCache cache,
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        return cache.SetAsync(
            key,
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }
}
