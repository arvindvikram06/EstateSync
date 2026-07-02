using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Interfaces.Services.Payments;

public interface ITenantPaymentService
{
    Task<string> CreateTenantCheckoutSessionAsync(Guid leaseBillId, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<LeaseBillResponse>> GetTenantBillsAsync(Guid tenantId, CancellationToken ct = default);
    Task HandleCheckoutCompletedAsync(string sessionJson, CancellationToken ct = default);
    Task HandleSessionExpiredAsync(string sessionJson, CancellationToken ct = default);
}
