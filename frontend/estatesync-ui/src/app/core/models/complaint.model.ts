export interface ComplaintResponse {
  complaintId: string;
  propertyId: string;
  propertyTitle: string;
  title: string;
  description: string;
  type: 'Maintenance' | 'Billing' | 'Noise' | 'Safety' | 'Cleanliness' | 'Other';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  createdAt: string;
  updatedAt: string;
}

export interface CreateComplaintRequest {
  propertyId: string;
  title: string;
  description: string;
  type: ComplaintResponse['type'];
  priority: ComplaintResponse['priority'];
}
