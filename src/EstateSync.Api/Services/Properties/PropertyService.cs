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
    private readonly IRepository<Location> _locationRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<AgreementType> _agreementTypeRepo;
    private readonly PropertyMapper _mapper;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IPropertyRepository propertyRepo,
        IRepository<Location> locationRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        IRepository<AgreementType> agreementTypeRepo,
        PropertyMapper mapper,
        ILogger<PropertyService> logger)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _locationRepo = locationRepo ?? throw new ArgumentNullException(nameof(locationRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _agreementTypeRepo = agreementTypeRepo ?? throw new ArgumentNullException(nameof(agreementTypeRepo));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResponse<PropertySummaryResponse>> SearchAsync(PropertySearchRequest request, CancellationToken ct)
    {
        var offset = (request.Page - 1) * request.Limit;
        var (items, total) = await _propertyRepo.SearchAsync(
            request.State,
            request.PropertyType,
            request.Bedrooms,
            request.MinRent,
            request.MaxRent,
            request.Lat,
            request.Lng,
            request.RadiusKm,
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


    public async Task<List<PropertySummaryResponse>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct)
    {
        var properties = await _propertyRepo.GetByOwnerIdAsync(ownerId);
        return properties.Select(_mapper.MapToSummaryResponse).ToList();
    }

    public async Task<Guid> CreateAsync(CreatePropertyRequest request, Guid ownerId, CancellationToken ct)
    {
        var isVerified = await _userRepo.IsUserVerifiedOwnerAsync(ownerId);
        if (!isVerified)
        {
            throw new ForbiddenException("Owner must be verified before listing a property.");
        }

        var location = await _locationRepo.GetByIdAsync(request.LocationId);
        if (location == null)
        {
            throw new NotFoundException("Location", request.LocationId);
        }

        if (request.AgreementTypeId.HasValue)
        {
            var agreementType = await _agreementTypeRepo.GetByIdAsync(request.AgreementTypeId.Value);
            if (agreementType == null)
            {
                throw new NotFoundException("AgreementType", request.AgreementTypeId.Value);
            }
        }

        var property = _mapper.MapCreateRequestToEntity(request);
        property.OwnerId = ownerId;
        property.Status = PropertyStatus.Draft;
        property.CreatedAt = DateTime.UtcNow;

        await _propertyRepo.AddAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Property {PropertyId} created successfully in Draft status by owner {OwnerId}", property.PropertyId, ownerId);

        return property.PropertyId;
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

        var location = await _locationRepo.GetByIdAsync(request.LocationId);
        if (location == null)
        {
            throw new NotFoundException("Location", request.LocationId);
        }

        if (request.AgreementTypeId.HasValue)
        {
            var agreementType = await _agreementTypeRepo.GetByIdAsync(request.AgreementTypeId.Value);
            if (agreementType == null)
            {
                throw new NotFoundException("AgreementType", request.AgreementTypeId.Value);
            }
        }

        _mapper.MapUpdateRequestToEntity(request, property);
        property.UpdatedAt = DateTime.UtcNow;

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Property {PropertyId} updated successfully by owner {OwnerId}", id, ownerId);

        return _mapper.MapToDetailResponse(property);
    }

    public async Task DeleteAsync(Guid id, Guid userId, string userRole, CancellationToken ct)
    {
        var property = await _propertyRepo.GetByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (userRole != "Admin" && property.OwnerId != userId)
        {
            throw new ForbiddenException("You do not have permission to delete this property.");
        }

        await _propertyRepo.DeleteAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Property {PropertyId} deleted by user {UserId} with role {Role}", id, userId, userRole);
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
