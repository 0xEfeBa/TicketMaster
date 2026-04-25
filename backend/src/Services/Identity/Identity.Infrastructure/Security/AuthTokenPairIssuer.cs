using System.Security.Cryptography;
using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security;

public sealed class AuthTokenPairIssuer(
    IJwtTokenIssuer jwt,
    IRefreshTokenStore refreshStore,
    IOptions<JwtOptions> jwtOptions) : IAuthTokenPairIssuer
{
    private readonly JwtOptions _opt = jwtOptions.Value;

    public async Task<AuthResponse> IssueForUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var access = jwt.CreateAccessToken(user);
        var refreshPlain = CreateRefreshToken();
        var lifetime = TimeSpan.FromDays(Math.Max(1, _opt.RefreshTokenDays));
        await refreshStore.StoreAsync(user.Id, refreshPlain, lifetime, cancellationToken);
        var refreshSec = (int)lifetime.TotalSeconds;
        return new AuthResponse(access.Token, access.ExpiresInSeconds, "Bearer", refreshPlain, refreshSec);
    }

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
