using System.Security.Claims;

namespace Identity.Application.Abstractions;

public interface ILogoutService
{
    Task ExecuteAsync(ClaimsPrincipal principal, string? refreshTokenPlaintext, CancellationToken cancellationToken = default);
}
