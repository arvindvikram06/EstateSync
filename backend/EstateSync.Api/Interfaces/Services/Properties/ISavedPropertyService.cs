using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface ISavedPropertyService
{
    Task SaveAsync(Guid userId, Guid propertyId, CancellationToken ct = default);
    Task UnsaveAsync(Guid userId, Guid propertyId, CancellationToken ct = default);
    Task<PagedResponse<PropertySummaryResponse>> GetSavedAsync(Guid userId, int page, int limit, CancellationToken ct = default);
    Task<List<Guid>> GetSavedIdsAsync(Guid userId, CancellationToken ct = default);
}
