using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Api.Services.Properties;

public class PropertyAmenityService : IPropertyAmenityService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyAmenityService> _logger;

    public PropertyAmenityService(
        IPropertyRepository propertyRepo,
        IUnitOfWork unitOfWork,
        ILogger<PropertyAmenityService> _logger)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
    }

    public async Task AddAmenitiesAsync(Guid id, List<string> amenityNames, Guid ownerId, CancellationToken ct)
    {
        if (amenityNames == null || !amenityNames.Any())
        {
            throw new BadRequestException("No amenity names specified.");
        }

        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        var cleanNames = amenityNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Replace(" ", "").ToLowerInvariant());

        property.Amenities = property.Amenities
            .Concat(cleanNames)
            .Distinct()
            .ToList();

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Added amenities for property {PropertyId} by owner {OwnerId}", id, ownerId);
    }

    public async Task UpdateAmenitiesAsync(Guid id, List<string> amenityNames, Guid ownerId, CancellationToken ct)
    {
        if (amenityNames == null)
        {
            throw new BadRequestException("Amenity list cannot be null.");
        }

        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        property.Amenities = amenityNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Replace(" ", "").ToLowerInvariant())
            .Distinct()
            .ToList();

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated amenities list for property {PropertyId} by owner {OwnerId}", id, ownerId);
    }

    public async Task DeleteAmenitiesAsync(Guid id, List<string> amenityNames, Guid ownerId, CancellationToken ct)
    {
        if (amenityNames == null || !amenityNames.Any())
        {
            throw new BadRequestException("No amenity names specified for removal.");
        }

        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        var toRemove = amenityNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Replace(" ", "").ToLowerInvariant())
            .ToHashSet();

        property.Amenities = property.Amenities
            .Where(a => !toRemove.Contains(a))
            .ToList();

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Removed amenities from property {PropertyId} by owner {OwnerId}", id, ownerId);
    }
}
