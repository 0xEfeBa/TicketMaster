namespace Identity.Application.Abstractions;

public interface IAccessTokenBlacklist
{
    Task AddAsync(string jwtId, TimeSpan timeToLive, CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(string jwtId, CancellationToken cancellationToken = default);
}
