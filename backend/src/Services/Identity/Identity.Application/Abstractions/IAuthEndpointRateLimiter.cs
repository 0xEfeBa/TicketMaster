namespace Identity.Application.Abstractions;

public interface IAuthEndpointRateLimiter
{
    Task<bool> AllowLoginAsync(string clientKey, CancellationToken cancellationToken = default);

    Task<bool> AllowRegisterAsync(string clientKey, CancellationToken cancellationToken = default);
}
