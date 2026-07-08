using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IPropertyRepository : IRepository<Property>
{
    Task<Property?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<Property>> GetByOwnerIdAsync(Guid ownerId);
    Task<(IEnumerable<Property> Items, int Total)> SearchAsync(
        string? search,
        string? state,
        string? propertyType,
        int? bedrooms,
        decimal? minRent,
        decimal? maxRent,
        int limit,
        int offset);
    Task<(IEnumerable<Property> Items, int Total)> GetPendingApprovalPropertiesAsync(int limit, int offset);
    Task<Property?> GetActiveDraftByOwnerIdAsync(Guid ownerId);
}
