namespace Identity.Application.Abstractions;

public interface IRefreshTokenStore
{
    Task StoreAsync(Guid userId, string refreshTokenPlaintext, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<Guid?> TryGetUserIdAsync(string refreshTokenPlaintext, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshTokenPlaintext, CancellationToken cancellationToken = default);
}
