export interface ComplaintImageResponse {
  complaintImageId: string;
  imageUrl: string;
}

export interface ComplaintResponse {
  complaintId: string;
  leaseId: string;
  propertyTitle: string | null;
  title: string;
  description: string;
  complaintType: 'Maintenance' | 'Billing' | 'Noise' | 'Safety' | 'Cleanliness' | 'Other';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'Open' | 'InProgress' | 'ResolutionProposed' | 'Reopened' | 'Resolved' | 'Closed';
  reviewRemarks: string | null;
  resolutionSummary: string | null;
  resolutionProposedAt: string | null;
  resolvedAt: string | null;
  reopenCount: number;
  lastReopenedAt: string | null;
  reopenReason: string | null;
  createdAt: string;
  updatedAt: string | null;
  images: ComplaintImageResponse[];
  activities: ComplaintActivityResponse[];
}

export interface ComplaintActivityResponse {
  activityId: string;
  actorName: string;
  actorRole: string;
  activityType: string;
  previousStatus: ComplaintResponse['status'] | null;
  newStatus: ComplaintResponse['status'] | null;
  summary: string | null;
  createdAt: string;
}

export interface CreateComplaintRequest {
  leaseId: string;
  title: string;
  description: string;
  complaintType: ComplaintResponse['complaintType'];
  priority: ComplaintResponse['priority'];
}
