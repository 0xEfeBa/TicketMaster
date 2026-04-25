using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}