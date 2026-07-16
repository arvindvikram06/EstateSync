import { fakeAsync, tick } from '@angular/core/testing';
import { DataRefreshService, refreshDomainsFor } from './data-refresh.service';

describe('DataRefreshService', () => {
  let service: DataRefreshService;

  beforeEach(() => {
    service = new DataRefreshService();
  });

  it('emits to subscribers of the triggered domain', fakeAsync(() => {
    let count = 0;
    const sub = service.on('visits').subscribe(() => count++);

    service.trigger('visits');
    tick(400);

    expect(count).toBe(1);
    sub.unsubscribe();
  }));

  it('does not emit to subscribers of other domains', fakeAsync(() => {
    let count = 0;
    const sub = service.on('leases').subscribe(() => count++);

    service.trigger('visits');
    tick(400);

    expect(count).toBe(0);
    sub.unsubscribe();
  }));

  it('collapses a burst of triggers into a single emission', fakeAsync(() => {
    let count = 0;
    const sub = service.on('applications').subscribe(() => count++);

    service.trigger('applications');
    service.trigger('applications');
    service.trigger('applications');
    tick(400);

    expect(count).toBe(1);
    sub.unsubscribe();
  }));
});

describe('refreshDomainsFor', () => {
  it('returns the correct domain for a known notification type', () => {
    expect(refreshDomainsFor('SiteVisitRequested')).toEqual(['visits']);
    expect(refreshDomainsFor('VerificationApproved')).toEqual(['profile']);
    expect(refreshDomainsFor('LeasePendingAdminApproval')).toEqual(['leases', 'admin-leases']);
  });

  it('returns empty array for unknown types without entityType', () => {
    expect(refreshDomainsFor('General')).toEqual([]);
  });

  it('falls back to properties domains when entityType is Property', () => {
    expect(refreshDomainsFor('General', 'Property')).toEqual(['properties', 'admin-properties']);
  });
});
