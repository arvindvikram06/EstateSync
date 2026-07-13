namespace EstateSync.Api.Models;

public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected,
    Withdrawn,
    // Lease lifecycle for this application finished (terminated/expired) —
    // the application can no longer be used to create another lease.
    Completed
}
