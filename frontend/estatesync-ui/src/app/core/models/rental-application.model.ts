export interface RentalApplicationResponse {
  applicationId: string;
  propertyId: string;
  propertyTitle: string;
  propertyPrimaryImage: string | null;
  tenantId: string;
  tenantName: string | null;
  tenantPhoneNumber: string | null;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
  message: string;
  expectedMoveInDate: string;
  durationMonths: number;
  commuteDistance: number;
  leaseId: string | null;
  leaseStatus: string | null;
  reviewRemarks: string | null;
  reviewedAt: string | null;
  appliedAt: string;
}

export interface CreateRentalApplicationRequest {
  message: string;
  expectedMoveInDate: string;
  durationMonths: number;
  commuteDistance: number;
}

export interface OwnerApplicationResponse {
  applicationId: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  tenantName: string;
  tenantEmail: string;
  tenantPhoneNumber: string | null;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
  leaseId: string | null;
  leaseStatus: string | null;
  appliedAt: string;
  reviewedAt: string | null;
  reviewRemarks: string | null;
  message: string;
  expectedMoveInDate: string;
  durationMonths: number;
  commuteDistance: number;
}

export interface ReviewApplicationRequest {
  approve: boolean;
  remarks?: string;
}
