using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class StripeCustomerProfile : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string StripeCustomerId { get; set; } // cus_xyz

    public User Tenant { get; set; } = null!;
}
