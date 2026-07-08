using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface IPropertyService
{
    Task<PagedResponse<PropertySummaryResponse>> SearchAsync(PropertySearchRequest request, CancellationToken ct);
    Task<PropertyDetailResponse> GetByIdAsync(Guid id, CancellationToken ct);

    Task<List<PropertyDetailResponse>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct);
    Task<PropertyDetailResponse> CreateAsync(CreatePropertyRequest request, Guid ownerId, CancellationToken ct);
    Task<PropertyDetailResponse> UpdateAsync(Guid id, UpdatePropertyRequest request, Guid ownerId, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct);
    Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid id, int page, int limit, CancellationToken ct);

    // Wizard draft endpoints — support partial creation and incremental saves
    Task<PropertyDetailResponse> CreateWizardDraftAsync(CreatePropertyDraftRequest request, Guid ownerId, CancellationToken ct);
    Task<PropertyDetailResponse> PatchWizardDraftAsync(Guid id, PatchPropertyDraftRequest request, Guid ownerId, CancellationToken ct);
    Task<PropertyDetailResponse?> GetMyActiveDraftAsync(Guid ownerId, CancellationToken ct);
}
