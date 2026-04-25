using Identity.Application.Abstractions;
using Identity.Domain.Enums;

namespace Identity.Application.Admin;

public class AssignRoleHandler(IUserRepository users)
{
    public async Task<(bool Ok, string? Error)> HandleAsync(Guid targetUserId, UserRole newRole, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(targetUserId, cancellationToken);
        if (user is null)
            return (false, "user_not_found");

        if (user.Role == UserRole.Admin)
            return (false, "admin_protected");

        if (newRole == UserRole.Admin)
            return (false, "admin_role_forbidden");

        user.Role = newRole;
        await users.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}