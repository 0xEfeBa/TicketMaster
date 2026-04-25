using Identity.Application.Auth;
using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IAuthTokenPairIssuer
{
    Task<AuthResponse> IssueForUserAsync(User user, CancellationToken cancellationToken = default);
}
