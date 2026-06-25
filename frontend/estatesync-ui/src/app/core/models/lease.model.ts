export interface LeaseSummaryResponse {
  leaseId: string;
  propertyId: string;
  propertyTitle: string;
  propertyAddress: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  status: 'PendingApproval' | 'PartialSigned' | 'PendingAdminApproval' | 'Active' | 'Expired' | 'Voided' | 'Terminated';
  tenantSignedAt: string | null;
  ownerSignedAt: string | null;
  agreementPdfUrl: string | null;
  signedPdfUrl: string | null;
}

export interface LeaseDetailResponse extends LeaseSummaryResponse {
  tenantName: string;
  ownerName: string;
  securityDeposit: number;
  paymentFrequency: 'Monthly' | 'Quarterly' | 'HalfYearly' | 'Yearly';
  penaltyPercentage: number;
  lockInPeriodMonths: number;
  signatureVerificationHash: string | null;
}

export interface SignLeaseRequest {
  signatureText: string;
  signatureImageBase64: string;
}
