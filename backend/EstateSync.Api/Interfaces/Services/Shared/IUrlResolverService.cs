using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Interfaces.Services.Shared;

public interface IUrlResolverService
{
    Task<PropertySummaryResponse> ResolveAsync(PropertySummaryResponse response, CancellationToken ct = default);
    Task<PropertyDetailResponse> ResolveAsync(PropertyDetailResponse response, CancellationToken ct = default);
    Task<LeaseResponse> ResolveAsync(LeaseResponse response, CancellationToken ct = default);
    Task<ComplaintResponse> ResolveAsync(ComplaintResponse response, CancellationToken ct = default);
}
