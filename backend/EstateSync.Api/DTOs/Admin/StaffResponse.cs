using System;

namespace EstateSync.Api.DTOs.Admin;

public record StaffResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt
);
