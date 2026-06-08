using System;

namespace EstateSync.Api.DTOs.Properties;

public record ReviewResponse
{
    public Guid ReviewId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid TenantId { get; init; }
    public required string TenantName { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
