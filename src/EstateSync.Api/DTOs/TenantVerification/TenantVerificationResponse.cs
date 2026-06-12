using System;

namespace EstateSync.Api.DTOs.TenantVerification;

public record TenantVerificationResponse : BaseVerificationResponse
{
    public Guid TenantId { get; init; }
    public Guid ApplicationId { get; init; }
}
