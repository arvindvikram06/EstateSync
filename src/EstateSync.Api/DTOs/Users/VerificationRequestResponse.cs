using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Users;

public record VerificationRequestResponse
{
    public Guid RequestId { get; init; }
    public Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Status { get; init; }
    public string? ReviewRemarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<DocumentResponse> Documents { get; init; } = new();
}
