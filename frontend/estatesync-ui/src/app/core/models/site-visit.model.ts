export interface SiteVisitResponse {
  siteVisitId: string;
  propertyId: string;
  propertyTitle: string | null;
  tenantId: string;
  tenantName: string | null;
  visitDate: string; // ISO string date
  visitTime: string; // e.g. "14:30"
  status: 'Pending' | 'Approved' | 'Declined' | 'Cancelled' | 'Completed';
  ownerNotes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateSiteVisitRequest {
  propertyId: string;
  visitDate: string; // ISO string Date component
  visitTime: string; // e.g., "14:30"
}

export interface UpdateSiteVisitStatusRequest {
  status: SiteVisitResponse['status'];
  ownerNotes: string | null;
}
