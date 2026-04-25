using Identity.Application.Abstractions;
using Identity.Domain.Entities;
namespace Identity.Application.Auth;

public class LoginUserHandler(IUserRepository users, IPasswordHasher passwordHasher)
{

public async Task<User?> HandleAsync(string email, string password, CancellationToken cancellationToken = default)
{
    var normalized = email.Trim().ToLowerInvariant();
    var user = await users.GetByEmailAsync(normalized, cancellationToken);
    if (user is null)
        return null;

    if (!passwordHasher.Verify(password, user.PasswordHash))
        return null;

    return user;
}





}
