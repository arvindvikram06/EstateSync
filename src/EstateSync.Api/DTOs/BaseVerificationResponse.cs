using System;
using System.Collections.Generic;
using EstateSync.Api.DTOs.Users;

namespace EstateSync.Api.DTOs;

public abstract record BaseVerificationResponse
{
    public Guid RequestId { get; init; }
    public required string Status { get; init; }
    public string? ReviewRemarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<DocumentResponse> Documents { get; init; } = new();
}
