using Booking.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Booking.Infrastructure.Services;

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private const string Prefix = "IdempotencyKey:";

    public RedisIdempotencyService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> RequestExistsAsync(Guid requestId)
    {
        var result = await _cache.GetStringAsync(Prefix + requestId.ToString());
        return !string.IsNullOrEmpty(result);
    }

    public async Task CreateRequestAsync(Guid requestId, string name)
    {
        await _cache.SetStringAsync(Prefix + requestId.ToString(), name, new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        });
    }
}
