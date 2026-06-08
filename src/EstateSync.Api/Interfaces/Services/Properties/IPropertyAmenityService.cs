using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface IPropertyAmenityService
{
    Task AddAmenitiesAsync(Guid propertyId, List<string> amenityNames, Guid ownerId, CancellationToken ct);
    Task UpdateAmenitiesAsync(Guid propertyId, List<string> amenityNames, Guid ownerId, CancellationToken ct);
    Task DeleteAmenitiesAsync(Guid propertyId, List<string> amenityNames, Guid ownerId, CancellationToken ct);
}
