using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Mappers;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Shared;

namespace EstateSync.Api.Services.Properties;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILeaseRepository _leaseRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PropertyMapper _mapper;
    private readonly ILogger<PropertyService> _logger;
    private readonly IUrlResolverService _urlResolver;
    private readonly ICacheService _cache;
    private static readonly TimeSpan ListingsCacheTtl = TimeSpan.FromMinutes(3);

    public PropertyService(
        IPropertyRepository propertyRepo,
        IUserRepository userRepo,
        ILeaseRepository leaseRepo,
        IUnitOfWork unitOfWork,
        PropertyMapper mapper,
        ILogger<PropertyService> logger,
        IUrlResolverService urlResolver,
        ICacheService cache)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PagedResponse<PropertySummaryResponse>> SearchAsync(PropertySearchRequest request, CancellationToken ct)
    {
        var version = await _cache.GetAsync<long>(PropertyCacheKeys.ListingsVersion, ct);
        if (version <= 0)
        {
            version = 1;
            await _cache.SetAsync(PropertyCacheKeys.ListingsVersion, version, TimeSpan.FromDays(30), ct);
        }

        var cacheKey = PropertyCacheKeys.Search(request, version);
        var cached = await _cache.GetAsync<PagedResponse<PropertySummaryResponse>>(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("Public property listing cache hit for {CacheKey}", cacheKey);
            return await ResolveSearchResponseAsync(cached, ct);
        }

        var offset = (request.Page - 1) * request.Limit;
        var (items, total) = await _propertyRepo.SearchAsync(
            request.Search,
            request.State,
            request.PropertyType,
            request.Bedrooms,
            request.MinRent,
            request.MaxRent,
            request.Limit,
            offset);

        var response = new PagedResponse<PropertySummaryResponse>
        {
            Success = true,
            Data = items.Select(_mapper.MapToSummaryResponse).ToList(),
            Page = request.Page,
            Limit = request.Limit,
            Total = total
        };
        await _cache.SetAsync(cacheKey, response, ListingsCacheTtl, ct);
        return await ResolveSearchResponseAsync(response, ct);
    }

    public async Task<PropertyDetailResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        return await _urlResolver.ResolveAsync(_mapper.MapToDetailResponse(property), ct);
    }


    public async Task<List<PropertyDetailResponse>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct)
    {
        var properties = await _propertyRepo.GetByOwnerIdAsync(ownerId);
        var result = new List<PropertyDetailResponse>();
        foreach (var p in properties)
            result.Add(await _urlResolver.ResolveAsync(_mapper.MapToDetailResponse(p), ct));
        return result;
    }

    public async Task<PropertyDetailResponse> CreateAsync(CreatePropertyRequest request, Guid ownerId, CancellationToken ct)
    {
        var isVerified = await _userRepo.IsUserVerifiedOwnerAsync(ownerId);
        if (!isVerified)
        {
            throw new ForbiddenException("Owner must be verified before listing a property.");
        }

        var property = _mapper.MapCreateRequestToEntity(request);
        property.OwnerId = ownerId;
        property.Status = PropertyStatus.Draft;
        property.CreatedAt = DateTime.UtcNow;

        await _propertyRepo.AddAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidatePublicListingsAsync(ct);

        _logger.LogInformation("Property {PropertyId} created successfully in Draft status by owner {OwnerId}", property.PropertyId, ownerId);

        
        property.Owner ??= await _userRepo.GetByIdAsync(ownerId);
        return _mapper.MapToDetailResponse(property);
    }

    public async Task<PropertyDetailResponse> UpdateAsync(Guid id, UpdatePropertyRequest request, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        _mapper.MapUpdateRequestToEntity(request, property);
        property.UpdatedAt = DateTime.UtcNow;

        // await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidatePublicListingsAsync(ct);

        _logger.LogInformation("Property {PropertyId} updated successfully by owner {OwnerId}", id, ownerId);

        return _mapper.MapToDetailResponse(property);
    }

    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var property = await _propertyRepo.GetByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (!isAdmin && property.OwnerId != userId)
        {
            throw new ForbiddenException("You do not have permission to delete this property.");
        }

        var activeLease = await _leaseRepo.GetActiveLeaseByPropertyIdAsync(id);
        if (activeLease != null)
        {
            throw new ConflictException("Cannot delete property because it has an active lease agreement.");
        }

        await _propertyRepo.DeleteAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidatePublicListingsAsync(ct);

        _logger.LogInformation("Property {PropertyId} deleted by user {UserId}. Admin: {IsAdmin}", id, userId, isAdmin);
    }

    public async Task<PropertyDetailResponse> CreateWizardDraftAsync(CreatePropertyDraftRequest request, Guid ownerId, CancellationToken ct)
    {
        var isVerified = await _userRepo.IsUserVerifiedOwnerAsync(ownerId);
        if (!isVerified)
        {
            throw new ForbiddenException("Owner must be verified (Stripe onboarded) before creating a property listing.");
        }

        var property = new Property
        {
            PropertyId  = Guid.NewGuid(),
            OwnerId     = ownerId,
            PropertyType = request.PropertyType,
            Status      = PropertyStatus.Draft,
            // Required DB columns — placeholder values; completion validated on submit
            Title       = string.Empty,
            Description = string.Empty,
            AddressLine = string.Empty,
            CreatedAt   = DateTime.UtcNow,
        };

        await _propertyRepo.AddAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Wizard draft property {PropertyId} created by owner {OwnerId}", property.PropertyId, ownerId);

        var created = await _propertyRepo.GetWithDetailsByIdAsync(property.PropertyId);
        return await _urlResolver.ResolveAsync(_mapper.MapToDetailResponse(created!), ct);
    }

    public async Task<PropertyDetailResponse> PatchWizardDraftAsync(Guid id, PatchPropertyDraftRequest request, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        if (property.Status != PropertyStatus.Draft)
        {
            throw new ConflictException("Only Draft properties can be updated via the wizard.");
        }

        var changed = _mapper.ApplyPatchToEntity(request, property);
        if (changed)
        {
            property.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            await InvalidatePublicListingsAsync(ct);
        }

        _logger.LogInformation("Wizard draft {PropertyId} patched by owner {OwnerId}", id, ownerId);

        return await _urlResolver.ResolveAsync(_mapper.MapToDetailResponse(property), ct);
    }

    public async Task<PropertyDetailResponse?> GetMyActiveDraftAsync(Guid ownerId, CancellationToken ct)
    {
        var draft = await _propertyRepo.GetActiveDraftByOwnerIdAsync(ownerId);
        return draft == null ? null : await _urlResolver.ResolveAsync(_mapper.MapToDetailResponse(draft), ct);
    }

    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid id, int page, int limit, CancellationToken ct)
    {
        var property = await _propertyRepo.GetByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        return new PagedResponse<ReviewResponse>
        {
            Success = true,
            Data = new List<ReviewResponse>(),
            Page = page,
            Limit = limit,
            Total = 0
        };
    }

    private async Task<PagedResponse<PropertySummaryResponse>> ResolveSearchResponseAsync(
        PagedResponse<PropertySummaryResponse> response,
        CancellationToken ct)
    {
        var resolved = new List<PropertySummaryResponse>();
        foreach (var item in response.Data ?? [])
            resolved.Add(await _urlResolver.ResolveAsync(item, ct));

        return new PagedResponse<PropertySummaryResponse>
        {
            Success = response.Success,
            Data = resolved,
            Page = response.Page,
            Limit = response.Limit,
            Total = response.Total
        };
    }

    private async Task InvalidatePublicListingsAsync(CancellationToken ct)
    {
        await _cache.IncrementAsync(PropertyCacheKeys.ListingsVersion, ct);
    }
}
