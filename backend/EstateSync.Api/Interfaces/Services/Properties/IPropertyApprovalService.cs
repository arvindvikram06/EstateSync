using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface IPropertyApprovalService
{
    Task SubmitForApprovalAsync(Guid id, Guid ownerId, CancellationToken ct);
    Task ApproveOrRejectAsync(Guid id, Guid adminId, string status, string remarks, CancellationToken ct);
    Task<PagedResponse<PropertySummaryResponse>> GetPendingApprovalPropertiesAsync(int page, int limit, CancellationToken ct);
}
