using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IJwtTokenIssuer
{
    AccessTokenResult CreateAccessToken(User user);
}
