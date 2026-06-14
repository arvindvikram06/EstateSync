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
            .Include(p => p.Owner)
            .Include(p => p.Location)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments)
                .ThenInclude(pd => pd.Document)
            .FirstOrDefaultAsync(p => p.PropertyId == id);
    }

    public async Task<IEnumerable<Property>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.Location)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments)
                .ThenInclude(pd => pd.Document)
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Property> Items, int Total)> SearchAsync(
        string? state,
        string? propertyType,
        int? bedrooms,
        decimal? minRent,
        decimal? maxRent,
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