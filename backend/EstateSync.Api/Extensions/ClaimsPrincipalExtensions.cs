using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace EstateSync.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }
}
