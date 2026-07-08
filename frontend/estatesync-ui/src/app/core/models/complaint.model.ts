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
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  reviewRemarks: string | null;
  createdAt: string;
  updatedAt: string | null;
  images: ComplaintImageResponse[];
}

export interface CreateComplaintRequest {
  leaseId: string;
  title: string;
  description: string;
  complaintType: ComplaintResponse['complaintType'];
  priority: ComplaintResponse['priority'];
}
