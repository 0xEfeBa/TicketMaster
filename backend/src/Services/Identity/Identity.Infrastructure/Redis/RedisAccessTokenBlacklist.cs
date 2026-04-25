using Identity.Application.Abstractions;
using StackExchange.Redis;

namespace Identity.Infrastructure.Redis;

public sealed class RedisAccessTokenBlacklist(IConnectionMultiplexer redis) : IAccessTokenBlacklist
{
    private const string KeyPrefix = "identity:bl:";
    private readonly IDatabase _db = redis.GetDatabase();

    public Task AddAsync(string jwtId, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        if (timeToLive <= TimeSpan.Zero)
            return Task.CompletedTask;
        var key = KeyPrefix + jwtId;
        return _db.StringSetAsync(key, "1", timeToLive);
    }

    public async Task<bool> ContainsAsync(string jwtId, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + jwtId;
        return await _db.KeyExistsAsync(key);
    }
}
