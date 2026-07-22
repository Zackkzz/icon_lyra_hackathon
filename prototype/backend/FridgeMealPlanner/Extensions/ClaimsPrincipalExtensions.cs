using System.Security.Claims;

namespace FridgeMealPlanner.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (int.TryParse(value, out var id)) return id;
        throw new UnauthorizedAccessException("No valid user id in token.");
    }
}
