namespace EstateSync.Api.Models;

public enum DocumentType
{
    // KYC documents
    AadhaarFront,
    AadhaarBack,
    PanCard,
    AddressProof,

    // Owner property verification documents
    PropertyDeed,
    PropertyTaxReceipt,
    OwnershipCertificate,

    // Tenant financial profile documents
    SalarySlip,
    BankStatement,
    EmploymentLetter,
    IncomeTaxReturn,

    // Legacy values kept for backward compatibility
    Aadhaar,
    PAN,
    Passport,
    VoterID,
    DrivingLicense,
    LeaseAgreement,
    RentalAgreement,
    BankProof,
    OwnerShipProof
}
