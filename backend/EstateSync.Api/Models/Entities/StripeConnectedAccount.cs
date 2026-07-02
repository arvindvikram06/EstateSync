using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class StripeConnectedAccount : BaseEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public required string StripeAccountId { get; set; } // acct_xyz

    // Persisted from account.updated webhook — avoids live Stripe API calls in admin views
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    public User Owner { get; set; } = null!;
}
