using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}