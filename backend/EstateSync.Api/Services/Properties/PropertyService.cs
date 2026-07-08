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

namespace EstateSync.Api.Services.Properties;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILeaseRepository _leaseRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PropertyMapper _mapper;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IPropertyRepository propertyRepo,
        IUserRepository userRepo,
        ILeaseRepository leaseRepo,
        IUnitOfWork unitOfWork,
        PropertyMapper mapper,
        ILogger<PropertyService> logger)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResponse<PropertySummaryResponse>> SearchAsync(PropertySearchRequest request, CancellationToken ct)
    {
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

        var mapped = items.Select(_mapper.MapToSummaryResponse).ToList();

        return new PagedResponse<PropertySummaryResponse>
        {
            Success = true,
            Data = mapped,
            Page = request.Page,
            Limit = request.Limit,
            Total = total
        };
    }

    public async Task<PropertyDetailResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        return _mapper.MapToDetailResponse(property);
    }


    public async Task<List<PropertyDetailResponse>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct)
    {
        var properties = await _propertyRepo.GetByOwnerIdAsync(ownerId);
        return properties.Select(_mapper.MapToDetailResponse).ToList();
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

        _logger.LogInformation("Property {PropertyId} created successfully in Draft status by owner {OwnerId}", property.PropertyId, ownerId);

        // The tracked entity is complete for a fresh Draft (no images/amenities
        // yet); only the Owner navigation needs populating for the response.
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
        return _mapper.MapToDetailResponse(created!);
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
        }

        _logger.LogInformation("Wizard draft {PropertyId} patched by owner {OwnerId}", id, ownerId);

        return _mapper.MapToDetailResponse(property);
    }

    public async Task<PropertyDetailResponse?> GetMyActiveDraftAsync(Guid ownerId, CancellationToken ct)
    {
        var draft = await _propertyRepo.GetActiveDraftByOwnerIdAsync(ownerId);
        return draft == null ? null : _mapper.MapToDetailResponse(draft);
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
}
