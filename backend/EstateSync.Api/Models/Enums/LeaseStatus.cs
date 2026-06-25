namespace EstateSync.Api.Models;

public enum LeaseStatus
{
    PendingApproval = 0,        // Owner created lease; awaiting signatures
    Active = 1,                  // Admin approved; lease is live
    Expired = 2,                 // End date passed
    Terminated = 3,              // Manually terminated
    PendingAdminApproval = 4,   // Both parties signed; awaiting admin review
    PartialSigned = 5,           // One party has signed; waiting for the other
    Voided = 6                   // Cancelled before activation (owner or admin rejection)
}
