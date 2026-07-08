using System;
using EstateSync.Api.Common;
using EstateSync.Api.Models.Enums;

namespace EstateSync.Api.Models;

public class OwnerProfile : BaseEntity
{
    public Guid OwnerProfileId { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public OwnerType OwnerType { get; set; } = OwnerType.Individual;

    // Company-only fields (null when OwnerType == Individual)
    public string? CompanyName { get; set; }
    public CompanyType? CompanyType { get; set; }
    public string? CIN { get; set; }
    public string? GSTIN { get; set; }

    public DateTime OwnerSince { get; set; }
}
