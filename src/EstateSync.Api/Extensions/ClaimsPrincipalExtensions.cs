using System;
using System.Security.Claims;

namespace EstateSync.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    public static string GetUserRole(this ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin")) return "Admin";
        if (user.IsInRole("Owner")) return "Owner";
        if (user.IsInRole("Tenant")) return "Tenant";
        return user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    public static System.Collections.Generic.IEnumerable<string> GetUserRoles(this ClaimsPrincipal user)
    {
        return System.Linq.Enumerable.Select(user.FindAll(ClaimTypes.Role), c => c.Value);
    }
}
