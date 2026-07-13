using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ISavedPropertyRepository : IRepository<SavedProperty>
{
    Task<(IEnumerable<SavedProperty> Items, int Total)> GetByUserAsync(Guid userId, int limit, int offset);
    Task<SavedProperty?> GetByUserAndPropertyAsync(Guid userId, Guid propertyId);
    Task<List<Guid>> GetSavedPropertyIdsAsync(Guid userId);
}
