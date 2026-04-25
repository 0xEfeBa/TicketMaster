using Identity.Application.Abstractions;
using StackExchange.Redis;

namespace Identity.Infrastructure.Redis;

public sealed class RedisRefreshTokenStore(IConnectionMultiplexer redis) : IRefreshTokenStore
{
    private const string KeyPrefix = "identity:rt:";
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task StoreAsync(
        Guid userId,
        string refreshTokenPlaintext,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + TokenKeyHelper.HashRefreshToken(refreshTokenPlaintext);
        await _db.StringSetAsync(key, userId.ToString(), lifetime);
    }

    public async Task<Guid?> TryGetUserIdAsync(string refreshTokenPlaintext, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + TokenKeyHelper.HashRefreshToken(refreshTokenPlaintext);
        var val = await _db.StringGetAsync(key);
        if (val.IsNullOrEmpty)
            return null;
        return Guid.TryParse(val!, out var id) ? id : null;
    }

    public async Task RevokeAsync(string refreshTokenPlaintext, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + TokenKeyHelper.HashRefreshToken(refreshTokenPlaintext);
        await _db.KeyDeleteAsync(key);
    }
}
