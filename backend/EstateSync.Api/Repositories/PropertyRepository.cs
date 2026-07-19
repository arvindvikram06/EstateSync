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
            .Include(p => p.Owner).ThenInclude(o => o!.OwnerProfile)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments)
                .ThenInclude(pd => pd.Document)
            .FirstOrDefaultAsync(p => p.PropertyId == id);
    }

    public async Task<IEnumerable<Property>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Owner).ThenInclude(o => o!.OwnerProfile)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments)
                .ThenInclude(pd => pd.Document)
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Property> Items, int Total)> SearchAsync(
        string? search,
        string? state,
        string? propertyType,
        int? bedrooms,
        decimal? minRent,
        decimal? maxRent,
        int limit,
        int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.PropertyImages)
            .Where(p => p.Status == PropertyStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in tokens)
            {
                var pattern = $"%{token}%";
                // Also match when spaces are stripped from both sides ("tamilnadu" → "Tamil Nadu")
                var strippedToken = token.Replace(" ", "");
                var strippedPattern = $"%{strippedToken}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Title, pattern) ||
                    EF.Functions.ILike(p.City, pattern) ||
                    EF.Functions.ILike(p.State, pattern) ||
                    EF.Functions.ILike(p.Title.Replace(" ", ""), strippedPattern) ||
                    EF.Functions.ILike(p.City.Replace(" ", ""), strippedPattern) ||
                    EF.Functions.ILike(p.State.Replace(" ", ""), strippedPattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(p => EF.Functions.ILike(p.State, state.Trim()));

        if (minRent.HasValue)
            query = query.Where(p => p.MonthlyRent >= minRent.Value);

        if (maxRent.HasValue)
            query = query.Where(p => p.MonthlyRent <= maxRent.Value);

        if (bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms >= bedrooms.Value);

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

    public async Task<Property?> GetActiveDraftByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .Include(p => p.Owner).ThenInclude(o => o!.OwnerProfile)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyDocuments).ThenInclude(pd => pd.Document)
            .Where(p => p.OwnerId == ownerId && p.Status == PropertyStatus.Draft)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<Property> Items, int Total)> GetPendingApprovalPropertiesAsync(int limit, int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.PropertyImages)
            .Include(p => p.Owner).ThenInclude(o => o!.OwnerProfile)
            .Where(p => p.Status == PropertyStatus.PendingApproval);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<Property> Items, int Total)> GetAllAdminAsync(string? search, string? status, int limit, int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.PropertyImages)
            .Include(p => p.Owner).ThenInclude(o => o!.OwnerProfile)
            .Where(p => p.Status != PropertyStatus.Draft)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(lower) ||
                p.City.ToLower().Contains(lower) ||
                p.State.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PropertyStatus>(status, true, out var parsedStatus))
            query = query.Where(p => p.Status == parsedStatus);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}
