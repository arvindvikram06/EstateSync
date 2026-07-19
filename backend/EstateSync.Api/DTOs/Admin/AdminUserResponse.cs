using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Admin;

public record AdminUserResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsEmailVerified,
    bool IsKycVerified,
    bool IsOwnerVerified,
    List<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLogin
);
