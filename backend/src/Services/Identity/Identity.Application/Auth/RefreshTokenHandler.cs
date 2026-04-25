using Identity.Application.Abstractions;
using Identity.Domain.Entities;

namespace Identity.Application.Auth;

public class RefreshTokenHandler(
    IUserRepository users,
    IRefreshTokenStore refreshStore,
    IAuthTokenPairIssuer pairIssuer)
{
    public async Task<(AuthResponse? Response, string? Error)> HandleAsync(
        string refreshTokenPlaintext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlaintext))
            return (null, "invalid_refresh");

        var userId = await refreshStore.TryGetUserIdAsync(refreshTokenPlaintext, cancellationToken);
        if (userId is null)
            return (null, "invalid_refresh");

        var user = await users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return (null, "invalid_refresh");

        await refreshStore.RevokeAsync(refreshTokenPlaintext, cancellationToken);
        var response = await pairIssuer.IssueForUserAsync(user, cancellationToken);
        return (response, null);
    }
}
