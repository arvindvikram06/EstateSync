export type LeaseStatusString =
  | 'PendingApproval'
  | 'PartialSigned'
  | 'PendingAdminApproval'
  | 'Active'
  | 'Expired'
  | 'Voided'
  | 'Terminated';

export interface LeaseSummaryResponse {
  leaseId: string;
  propertyId: string;
  propertyTitle: string | null;
  propertyAddress: string | null;
  tenantId: string;
  ownerId: string | null;
  tenantName: string | null;
  ownerName: string | null;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  status: LeaseStatusString;
  isRenewalRequested: boolean;
  tenantSignedAt: string | null;
  ownerSignedAt: string | null;
  agreementPdfUrl: string | null;
}

export interface OwnerLeaseSummaryResponse extends LeaseSummaryResponse {
  tenantEmail: string | null;
}

export interface LeaseDetailResponse extends LeaseSummaryResponse {
  tenantEmail: string | null;
  applicationId: string;
  securityDeposit: number;
  paymentFrequency: 'Monthly' | 'Quarterly' | 'HalfYearly' | 'Yearly';
  noticePeriodDays: number | null;
  penaltyPercentage: number | null;
  agreementText: string | null;
  tenantSignature: string | null;
  ownerSignature: string | null;
  tenantSignatureImageUrl: string | null;
  ownerSignatureImageUrl: string | null;
  adminApprovedAt: string | null;
  adminRemarks: string | null;
  activatedAt: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
  cancelledAt: string | null;
}

export interface CreateLeaseRequest {
  applicationId: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  securityDeposit: number;
  paymentFrequency: string;
  noticePeriodDays: number;
  penaltyPercentage: number;
}

export interface SignLeaseRequest {
  signatureText: string;
  signatureImageBase64: string;
}

export interface VoidLeaseRequest {
  reason: string;
}

export interface TerminateLeaseRequest {
  reason: string;
  forfeitDeposit: boolean;
}

export interface AdminLeaseApprovalRequest {
  approve: boolean;
  remarks?: string;
}

export interface SubmitVacateNoticeRequest {
  requestedExitDate: string; // ISO date string
}

export interface RejectVacateNoticeRequest {
  reason: string;
}

export interface LeaseVacateNoticeResponse {
  vacateNoticeId: string;
  leaseId: string;
  requestedExitDate: string;
  status: 'Pending' | 'Accepted' | 'Rejected';
  penaltyApplied: boolean;
  penaltyAmount: number;
  rejectionReason: string | null;
  createdAt: string;
}
