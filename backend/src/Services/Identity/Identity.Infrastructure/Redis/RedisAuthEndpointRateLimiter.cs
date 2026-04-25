using Identity.Application.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Identity.Infrastructure.Redis;

public sealed class RedisAuthEndpointRateLimiter(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> options) : IAuthEndpointRateLimiter
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly RedisOptions _opt = options.Value;

    public async Task<bool> AllowLoginAsync(string clientKey, CancellationToken cancellationToken = default)
    {
        var limit = Math.Max(1, _opt.LoginRequestsPerMinute);
        return await AllowAsync($"identity:rl:login:{clientKey}", limit, cancellationToken);
    }

    public async Task<bool> AllowRegisterAsync(string clientKey, CancellationToken cancellationToken = default)
    {
        var limit = Math.Max(1, _opt.RegisterRequestsPerMinute);
        return await AllowAsync($"identity:rl:reg:{clientKey}", limit, cancellationToken);
    }

    private async Task<bool> AllowAsync(string key, int maxPerWindow, CancellationToken cancellationToken)
    {
        var count = await _db.StringIncrementAsync(key);
        if (count == 1)
            await _db.KeyExpireAsync(key, TimeSpan.FromMinutes(1));
        return count <= maxPerWindow;
    }
}
