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
