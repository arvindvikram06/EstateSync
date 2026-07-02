export interface RentalApplicationResponse {
  applicationId: string;
  propertyId: string;
  propertyTitle: string;
  propertyPrimaryImage: string | null;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
  appliedAt: string;
  reviewedAt: string | null;
  reviewRemarks: string | null;
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
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';
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
