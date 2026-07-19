using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Shared;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Models.Enums;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Properties;

public class SavedPropertyService : ISavedPropertyService
{
    private readonly ISavedPropertyRepository _savedPropertyRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PropertyMapper _mapper;
    private readonly IUrlResolverService _urlResolver;

    public SavedPropertyService(
        ISavedPropertyRepository savedPropertyRepo,
        IPropertyRepository propertyRepo,
        IUnitOfWork unitOfWork,
        PropertyMapper mapper,
        IUrlResolverService urlResolver)
    {
        _savedPropertyRepo = savedPropertyRepo ?? throw new ArgumentNullException(nameof(savedPropertyRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
    }

    public async Task SaveAsync(Guid userId, Guid propertyId, CancellationToken ct = default)
    {
        var property = await _propertyRepo.GetByIdAsync(propertyId)
            ?? throw new NotFoundException("Property", propertyId);

        if (property.Status != PropertyStatus.Active)
            throw new BadRequestException("Only active listings can be saved.");

        var existing = await _savedPropertyRepo.GetByUserAndPropertyAsync(userId, propertyId);
        if (existing != null) return;

        await _savedPropertyRepo.AddAsync(new SavedProperty
        {
            SavedPropertyId = Guid.NewGuid(),
            UserId = userId,
            PropertyId = propertyId,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent save hit the unique (UserId, PropertyId) index — already saved.
        }
    }

    public async Task UnsaveAsync(Guid userId, Guid propertyId, CancellationToken ct = default)
    {
        var existing = await _savedPropertyRepo.GetByUserAndPropertyAsync(userId, propertyId);
        if (existing == null) return;

        await _savedPropertyRepo.DeleteAsync(existing);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<PagedResponse<PropertySummaryResponse>> GetSavedAsync(Guid userId, int page, int limit, CancellationToken ct = default)
    {
        var offset = (page - 1) * limit;
        var (items, total) = await _savedPropertyRepo.GetByUserAsync(userId, limit, offset);

        var resolved = new List<PropertySummaryResponse>();
        foreach (var sp in items.Where(sp => sp.Property != null))
            resolved.Add(await _urlResolver.ResolveAsync(_mapper.MapToSummaryResponse(sp.Property!), ct));

        return new PagedResponse<PropertySummaryResponse>
        {
            Data = resolved,
            Page = page,
            Limit = limit,
            Total = total,
        };
    }

    public async Task<List<Guid>> GetSavedIdsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _savedPropertyRepo.GetSavedPropertyIdsAsync(userId);
    }
}
