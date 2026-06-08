using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Users;

public record SubmitVerificationRequest
{
    public List<Guid> DocumentIds { get; init; } = new();
}
