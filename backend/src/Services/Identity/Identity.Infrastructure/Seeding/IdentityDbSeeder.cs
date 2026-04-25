using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Seeding;

public static class IdentityDbSeeder
{
    public static async Task SeedAdminIfConfiguredAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var email = configuration["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(IdentityDbSeeder));

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return;

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed admin oluşturuldu: {Email}", email);
    }
}
