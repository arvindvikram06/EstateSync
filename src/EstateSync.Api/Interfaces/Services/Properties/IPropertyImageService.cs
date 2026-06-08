using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface IPropertyImageService
{
    Task<List<Guid>> UploadImagesAsync(Guid propertyId, List<IFormFile> files, Guid ownerId, CancellationToken ct);
    Task DeleteImageAsync(Guid propertyId, Guid imageId, Guid ownerId, CancellationToken ct);
}
