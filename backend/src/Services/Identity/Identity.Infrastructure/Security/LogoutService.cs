using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security;

public sealed class LogoutService(
    IAccessTokenBlacklist blacklist,
    IRefreshTokenStore refreshStore,
    IDistributedCache cache,
    IOptions<JwtOptions> jwtOptions) : ILogoutService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task ExecuteAsync(
        ClaimsPrincipal principal,
        string? refreshTokenPlaintext,
        CancellationToken cancellationToken = default)
    {
        var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                  ?? principal.FindFirst("jti")?.Value;
        var expUnix = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
                      ?? principal.FindFirst("exp")?.Value;

        if (!string.IsNullOrEmpty(jti))
        {
            var ttl = ResolveBlacklistTtl(expUnix);
            await blacklist.AddAsync(jti, ttl, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(refreshTokenPlaintext))
            await refreshStore.RevokeAsync(refreshTokenPlaintext, cancellationToken);

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (sub is not null && Guid.TryParse(sub, out var userId))
        {
            var cacheKey = UserMeCacheKey(userId);
            await cache.RemoveAsync(cacheKey, cancellationToken);
        }
    }

    private TimeSpan ResolveBlacklistTtl(string? expUnix)
    {
        if (expUnix is not null && long.TryParse(expUnix, out var unix))
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(unix);
            var ttl = exp - DateTimeOffset.UtcNow;
            if (ttl > TimeSpan.Zero)
                return ttl;
        }

        return TimeSpan.FromMinutes(Math.Max(1, _jwt.AccessTokenMinutes));
    }

    public static string UserMeCacheKey(Guid userId) => $"me:{userId}";
}
