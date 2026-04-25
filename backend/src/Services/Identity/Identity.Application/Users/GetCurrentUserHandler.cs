using Identity.Application.Abstractions;
using Identity.Application.Auth;

namespace Identity.Application.Users;

public class GetCurrentUserHandler(IUserRepository users)
{
    public async Task<UserMeResponse?> HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        return new UserMeResponse(user.Id, user.Email, user.Role.ToString());
    }
}