using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class TenantFinancialDocument : BaseEntity
{
    public Guid ProfileId { get; set; }
    public TenantFinancialProfile? TenantFinancialProfile { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
}
