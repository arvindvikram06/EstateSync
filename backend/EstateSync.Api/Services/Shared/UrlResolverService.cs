using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Interfaces.Services.Shared;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Api.Services.Shared;

public class UrlResolverService : IUrlResolverService
{
    private readonly IFileStorageService _storage;

    public UrlResolverService(IFileStorageService storage)
    {
        _storage = storage;
    }

    public async Task<PropertySummaryResponse> ResolveAsync(PropertySummaryResponse response, CancellationToken ct = default)
    {
        return response with
        {
            PrimaryImage = await _storage.GetFileUrlAsync(response.PrimaryImage ?? string.Empty, ct)
        };
    }

    public async Task<PropertyDetailResponse> ResolveAsync(PropertyDetailResponse response, CancellationToken ct = default)
    {
        var resolved = new List<PropertyImageResponse>(response.Images.Count);
        foreach (var img in response.Images)
            resolved.Add(img with { Image = await _storage.GetFileUrlAsync(img.Image, ct) });

        return response with { Images = resolved };
    }

    public async Task<LeaseResponse> ResolveAsync(LeaseResponse response, CancellationToken ct = default)
    {
        return response with
        {
            AgreementPdfUrl         = await _storage.GetFileUrlAsync(response.AgreementPdfUrl         ?? string.Empty, ct),
            TenantSignatureImageUrl = await _storage.GetFileUrlAsync(response.TenantSignatureImageUrl ?? string.Empty, ct),
            OwnerSignatureImageUrl  = await _storage.GetFileUrlAsync(response.OwnerSignatureImageUrl  ?? string.Empty, ct)
        };
    }

    public async Task<ComplaintResponse> ResolveAsync(ComplaintResponse response, CancellationToken ct = default)
    {
        foreach (var img in response.Images)
            img.ImageUrl = await _storage.GetFileUrlAsync(img.ImageUrl, ct);
        return response;
    }
}
