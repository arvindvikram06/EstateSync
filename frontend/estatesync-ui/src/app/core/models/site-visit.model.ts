export type SiteVisitStatus =
  | 'PendingOwnerApproval'
  | 'PendingTenantApproval'
  | 'Approved'
  | 'Declined'
  | 'Cancelled'
  | 'Completed';

export type VisitSlotStatus = 'Available' | 'Reserved' | 'Booked' | 'Blocked';

export interface PropertyVisitSlot {
  slotId: string;
  propertyId: string;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: VisitSlotStatus;
}

export interface SiteVisitResponse {
  siteVisitId: string;
  propertyId: string;
  propertyTitle: string | null;
  tenantId: string;
  tenantName: string | null;
  slotId: string | null;
  previousSlotId: string | null;
  visitDate: string;
  visitTime: string;
  durationMinutes: number;
  status: SiteVisitStatus;
  ownerNotes: string | null;
  rescheduledByUserId: string | null;
  rescheduledAt: string | null;
  rescheduleReason: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateSiteVisitRequest {
  propertyId: string;
  slotId: string;
}

export interface RescheduleSiteVisitRequest {
  newSlotId: string;
  reason: string;
}

export interface UpdateSiteVisitStatusRequest {
  status: SiteVisitStatus;
  ownerNotes: string | null;
}
