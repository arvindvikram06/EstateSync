import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { debounceTime, filter, map } from 'rxjs/operators';
import { NotificationType } from '../../models/enums';

export type RefreshDomain =
  | 'visits'
  | 'applications'
  | 'leases'
  | 'complaints'
  | 'bills'
  | 'properties'
  | 'admin-properties'
  | 'admin-leases'
  | 'profile';

const REFRESH_GROUPS: Partial<Record<NotificationType, RefreshDomain[]>> = {
  [NotificationType.SiteVisitRequested]: ['visits'],
  [NotificationType.SiteVisitApproved]: ['visits'],
  [NotificationType.SiteVisitDeclined]: ['visits'],
  [NotificationType.SiteVisitCancelled]: ['visits'],

  [NotificationType.ApplicationReceived]: ['applications'],
  [NotificationType.ApplicationApproved]: ['applications'],
  [NotificationType.ApplicationRejected]: ['applications'],

  [NotificationType.LeaseCreated]: ['leases'],
  [NotificationType.LeaseExpiring]: ['leases'],
  [NotificationType.LeaseTerminated]: ['leases'],
  [NotificationType.LeaseExpired]: ['leases'],
  [NotificationType.LeaseApprovedByAdmin]: ['leases'],
  [NotificationType.LeaseRejectedByAdmin]: ['leases'],
  [NotificationType.LeaseVoided]: ['leases'],
  [NotificationType.LeaseRenewalRequested]: ['leases'],
  [NotificationType.LeaseRenewalApproved]: ['leases'],
  [NotificationType.LeaseRenewalRejected]: ['leases'],
  [NotificationType.VacateNoticeSubmitted]: ['leases'],
  [NotificationType.VacateNoticeAccepted]: ['leases'],
  [NotificationType.VacateNoticeRejected]: ['leases'],
  [NotificationType.DepositForfeited]: ['leases'],
  [NotificationType.DepositReturnReminder]: ['leases'],

  [NotificationType.LeasePendingAdminApproval]: ['leases', 'admin-leases'],

  [NotificationType.ComplaintRaised]: ['complaints'],
  [NotificationType.ComplaintUpdated]: ['complaints'],
  [NotificationType.ComplaintResolved]: ['complaints'],

  [NotificationType.RentDue]: ['bills'],
  [NotificationType.PaymentReceived]: ['bills'],
  [NotificationType.PaymentFailed]: ['bills'],

  [NotificationType.VerificationApproved]: ['profile'],
  [NotificationType.VerificationRejected]: ['profile'],
};

export function refreshDomainsFor(type: string, entityType?: string | null): RefreshDomain[] {
  const mapped = REFRESH_GROUPS[type as NotificationType];
  if (mapped) return mapped;
  if (entityType === 'Property') return ['properties', 'admin-properties'];
  return [];
}

@Injectable({ providedIn: 'root' })
export class DataRefreshService {
  private readonly refreshSubject = new Subject<RefreshDomain>();

  trigger(domain: RefreshDomain): void {
    this.refreshSubject.next(domain);
  }

  on(domain: RefreshDomain): Observable<void> {
    return this.refreshSubject.pipe(
      filter(d => d === domain),
      debounceTime(400),
      map(() => undefined)
    );
  }
}
