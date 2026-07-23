using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.SiteVisits;

namespace EstateSync.Api.Interfaces.Services.SiteVisits;

public interface ISiteVisitService
{
    Task<SiteVisitResponse> RequestVisitAsync(Guid tenantId, CreateSiteVisitRequest request, CancellationToken ct = default);
    Task<SiteVisitResponse> UpdateStatusAsync(Guid userId, Guid siteVisitId, UpdateSiteVisitStatusRequest request, CancellationToken ct = default);
    Task<IEnumerable<SiteVisitResponse>> GetTenantVisitsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<SiteVisitResponse>> GetOwnerVisitsAsync(Guid ownerId, CancellationToken ct = default);
    Task<SiteVisitResponse> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<SiteVisitResponse> RescheduleAsync(Guid userId, Guid siteVisitId, RescheduleSiteVisitRequest request, CancellationToken ct = default);
    Task<SiteVisitResponse> ConfirmRescheduleAsync(Guid userId, Guid siteVisitId, CancellationToken ct = default);
    Task<SiteVisitResponse> RejectRescheduleAsync(Guid userId, Guid siteVisitId, CancellationToken ct = default);
}
