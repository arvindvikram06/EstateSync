namespace EstateSync.Api.Models;

public enum SiteVisitStatus
{
    PendingOwnerApproval,
    PendingTenantApproval,
    Approved,
    Declined,
    Cancelled,
    Completed
}
