namespace EstateSync.Api.Models;

public enum NotificationType
{
    LeaseCreated,
    LeaseExpiring,
    LeaseTerminated,
    RentDue,
    PaymentReceived,
    PaymentFailed,
    ApplicationReceived,
    ApplicationApproved,
    ApplicationRejected,
    ComplaintRaised,
    ComplaintUpdated,
    ComplaintResolved,
    RentIncreaseProposed,
    RentIncreaseAccepted,
    RentIncreaseRelieved,
    VerificationApproved,
    VerificationRejected,
    DocumentUploaded,
    General
}
