using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<int> CountAdminsAsync(CancellationToken cancellationToken = default) =>
        db.Users.CountAsync(u => u.Role == UserRole.Admin, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        db.Users.AsTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await db.Users.AddAsync(user, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
