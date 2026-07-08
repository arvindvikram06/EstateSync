using EstateSync.Api.Models.Enums;

namespace EstateSync.Api.DTOs.Users;

public record BecomeOwnerRequest
{
    public OwnerType OwnerType { get; init; } = OwnerType.Individual;
    public string? CompanyName { get; init; }
    public CompanyType? CompanyType { get; init; }
    public string? CIN { get; init; }
    public string? GSTIN { get; init; }
}
