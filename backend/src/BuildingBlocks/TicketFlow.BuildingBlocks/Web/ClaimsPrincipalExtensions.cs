using System.Security.Claims;

namespace TicketFlow.BuildingBlocks.Web;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal? user, out Guid userId)
    {
        userId = default;
        if (user is null) return false;
        var s = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(s) && Guid.TryParse(s, out userId);
    }

    public static string? GetRoleName(this ClaimsPrincipal? user) =>
        user?.FindFirst(ClaimTypes.Role)?.Value;
}
