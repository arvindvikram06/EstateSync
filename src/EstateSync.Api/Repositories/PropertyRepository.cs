using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class PropertyRepository : BaseRepository<Property>, IPropertyRepository
{
    public PropertyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Property?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.Location)
            .Include(p => p.AgreementType)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments)
                .ThenInclude(pd => pd.Document)
            .FirstOrDefaultAsync(p => p.PropertyId == id);
    }

    public async Task<IEnumerable<Property>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Property> Items, int Total)> SearchAsync(
        string? state,
        string? propertyType,
        int? bedrooms,
        decimal? minRent,
        decimal? maxRent,
        decimal? latitude,
        decimal? longitude,
        decimal? radiusKm,
        int limit,
        int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Location)
            .Include(p => p.PropertyImages)
            .Where(p => p.Status == PropertyStatus.Active);

        if (!string.IsNullOrEmpty(state))
            query = query.Where(p => p.Location != null && p.Location.StateName.ToLower() == state.ToLower());

        if (minRent.HasValue)
            query = query.Where(p => p.MonthlyRent >= minRent.Value);

        if (maxRent.HasValue)
            query = query.Where(p => p.MonthlyRent <= maxRent.Value);

        if (bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms == bedrooms.Value);

        if (!string.IsNullOrEmpty(propertyType))
        {
            if (Enum.TryParse<PropertyType>(propertyType, true, out var parsedType))
            {
                query = query.Where(p => p.PropertyType == parsedType);
            }
        }

        if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
        {
            var latRad = (double)latitude.Value * Math.PI / 180.0;
            var lonRad = (double)longitude.Value * Math.PI / 180.0;

            query = query.Where(p =>
                6371 * Math.Acos(
                    Math.Cos(latRad) * Math.Cos((double)(p.Latitude ?? 0) * Math.PI / 180.0) *
                    Math.Cos(((double)(p.Longitude ?? 0) * Math.PI / 180.0) - lonRad) +
                    Math.Sin(latRad) * Math.Sin((double)(p.Latitude ?? 0) * Math.PI / 180.0)
                ) <= (double)radiusKm.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<Property> Items, int Total)> GetPendingApprovalPropertiesAsync(int limit, int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Location)
            .Include(p => p.PropertyImages)
            .Where(p => p.Status == PropertyStatus.PendingApproval);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}