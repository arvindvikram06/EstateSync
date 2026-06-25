using System;
using System.Linq;
using EstateSync.Api.DTOs.Auth;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class AuthMapper
{
    public AuthResponse MapToAuthResponse(User user, string accessToken)
    {
        return new AuthResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            AccessToken = accessToken,
            Email = user.Email,
            Roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name)
                .ToList()
        };
    }
}
