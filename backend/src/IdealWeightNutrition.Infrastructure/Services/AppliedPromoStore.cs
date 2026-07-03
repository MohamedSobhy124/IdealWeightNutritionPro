using System.Text.Json;
using IdealWeightNutrition.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AppliedPromoStore(IDistributedCache cache)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    private static string Key(string scope, string id) => $"cart-promo:{scope}:{id}";

    public async Task<AppliedPromo?> GetAsync(string scope, string id, CancellationToken ct)
    {
        var json = await cache.GetStringAsync(Key(scope, id), ct);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<AppliedPromo>(json, JsonOptions);
    }

    public Task SaveAsync(string scope, string id, AppliedPromo promo, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(promo, JsonOptions);
        return cache.SetStringAsync(
            Key(scope, id),
            json,
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(14) },
            ct);
    }

    public Task RemoveAsync(string scope, string id, CancellationToken ct) =>
        cache.RemoveAsync(Key(scope, id), ct);
}

internal sealed class AppliedPromo
{
    public int PromoCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
}
