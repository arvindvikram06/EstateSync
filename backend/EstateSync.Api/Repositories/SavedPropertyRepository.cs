using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class SavedPropertyRepository : BaseRepository<SavedProperty>, ISavedPropertyRepository
{
    public SavedPropertyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<SavedProperty> Items, int Total)> GetByUserAsync(Guid userId, int limit, int offset)
    {
        var query = _dbSet.AsNoTracking()
            .Include(sp => sp.Property)
                .ThenInclude(p => p!.PropertyImages)
            .Where(sp => sp.UserId == userId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(sp => sp.SavedPropertyId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<SavedProperty?> GetByUserAndPropertyAsync(Guid userId, Guid propertyId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.PropertyId == propertyId);
    }

    public async Task<List<Guid>> GetSavedPropertyIdsAsync(Guid userId)
    {
        return await _dbSet.AsNoTracking()
            .Where(sp => sp.UserId == userId)
            .Select(sp => sp.PropertyId)
            .ToListAsync();
    }
}
