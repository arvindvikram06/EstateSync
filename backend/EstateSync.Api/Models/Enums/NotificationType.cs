namespace EstateSync.Api.Models;

public enum NotificationType
{
    LeaseCreated,
    LeaseExpiring,
    LeaseTerminated,
    LeasePendingAdminApproval,
    LeaseApprovedByAdmin,
    LeaseRejectedByAdmin,
    LeaseVoided,
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
    General,
    LeaseExpired,
    DepositForfeited,
    DepositReturnReminder,
    LeaseRenewalRequested,
    LeaseRenewalApproved,
    LeaseRenewalRejected
}
