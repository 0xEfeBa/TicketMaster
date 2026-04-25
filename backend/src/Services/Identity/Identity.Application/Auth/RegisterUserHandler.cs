using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Auth;

public class RegisterUserHandler(IUserRepository users, IPasswordHasher passwordHasher)
{
    public async Task<(User? User, string? Error)> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
            return (null, "invalid_email");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return (null, "invalid_password");

        if (await users.ExistsByEmailAsync(email, cancellationToken))
            return (null, "email_taken");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Customer,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return (user, null);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}