using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class StripeConnectedAccount : BaseEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public required string StripeAccountId { get; set; } // acct_xyz

    public User Owner { get; set; } = null!;
}
