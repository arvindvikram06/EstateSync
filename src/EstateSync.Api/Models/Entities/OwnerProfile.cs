using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class OwnerProfile : BaseEntity
{
    public Guid OwnerProfileId { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string PanOrTaxId { get; set; }
}
