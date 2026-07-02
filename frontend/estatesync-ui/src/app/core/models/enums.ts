export enum PropertyType {
  IndependentHouse = 'IndependentHouse',
  Villa = 'Villa'
}

export enum PropertyStatus {
  Draft = 0,
  PendingApproval = 1,
  Active = 2,
  Rejected = 3,
  Rented = 4,
  Leased = 5,
  AvailableSoon = 6
}

export enum LeaseStatus {
  PendingApproval = 0,
  Active = 1,
  Expired = 2,
  Terminated = 3,
  PendingAdminApproval = 4,
  PartialSigned = 5,
  Voided = 6
}

export enum ApplicationStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Withdrawn = 3
}

export enum ApprovalStatus {
  Draft = 0,
  Pending = 1,
  Approved = 2,
  Rejected = 3
}

export enum PaymentFrequency {
  Monthly = 0,
  Quarterly = 1,
  HalfYearly = 2,
  Yearly = 3
}

export enum PaymentStatus {
  Pending = 0,
  Paid = 1,
  Failed = 2
}

export enum LeaseBillType {
  Rent = 0,
  Deposit = 1,
  Penalty = 2,
  Maintenance = 3,
  Other = 4
}

export enum ComplaintType {
  Maintenance,
  Billing,
  Noise,
  Safety,
  Cleanliness,
  Other
}

export enum ComplaintStatus {
  Open,
  InProgress,
  Resolved,
  Closed
}

export enum ComplaintPriority {
  Low,
  Medium,
  High,
  Critical
}

export enum DocumentType {
  AadhaarFront,
  AadhaarBack,
  PanCard,
  AddressProof,
  PropertyDeed,
  PropertyTaxReceipt,
  OwnershipCertificate,
  SalarySlip,
  BankStatement,
  EmploymentLetter,
  IncomeTaxReturn
}

export enum EmploymentType {
  Salaried,
  SelfEmployed,
  BusinessOwner,
  Student,
  Other
}

export enum NotificationType {
  LeaseCreated = 'LeaseCreated',
  LeaseExpiring = 'LeaseExpiring',
  LeaseTerminated = 'LeaseTerminated',
  LeaseExpired = 'LeaseExpired',
  LeasePendingAdminApproval = 'LeasePendingAdminApproval',
  LeaseApprovedByAdmin = 'LeaseApprovedByAdmin',
  LeaseRejectedByAdmin = 'LeaseRejectedByAdmin',
  LeaseVoided = 'LeaseVoided',
  RentDue = 'RentDue',
  PaymentReceived = 'PaymentReceived',
  PaymentFailed = 'PaymentFailed',
  ApplicationReceived = 'ApplicationReceived',
  ApplicationApproved = 'ApplicationApproved',
  ApplicationRejected = 'ApplicationRejected',
  ComplaintRaised = 'ComplaintRaised',
  ComplaintUpdated = 'ComplaintUpdated',
  ComplaintResolved = 'ComplaintResolved',
  VerificationApproved = 'VerificationApproved',
  VerificationRejected = 'VerificationRejected',
  PropertyApproved = 'PropertyApproved',
  PropertyRejected = 'PropertyRejected',
  DocumentUploaded = 'DocumentUploaded',
  General = 'General',
}
