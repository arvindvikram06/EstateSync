using System;
using System.Collections.Generic;

namespace EstateSync.Api.Models;

public class AgreementType
{
    public Guid AgreementTypeId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool AllowsRentIncrease { get; set; }
    public bool RequiresNoticePeriod { get; set; }
    public decimal? DefaultPenaltyPercentage { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<AgreementTemplate> AgreementTemplates { get; set; } = new List<AgreementTemplate>();
}
