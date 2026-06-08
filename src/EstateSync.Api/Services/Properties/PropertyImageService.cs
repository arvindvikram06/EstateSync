using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Api.Services.Properties;

public class PropertyImageService : IPropertyImageService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyImageService> _logger;

    public PropertyImageService(
        IPropertyRepository propertyRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ILogger<PropertyImageService> logger)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Guid>> UploadImagesAsync(Guid id, List<IFormFile> files, Guid ownerId, CancellationToken ct)
    {
        if (files == null || !files.Any())
        {
            throw new BadRequestException("No files uploaded.");
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

        var existingCount = property.PropertyImages.Count;
        if (existingCount + files.Count > 10)
        {
            throw new BadRequestException("A property can have a maximum of 10 images.");
        }

        var newImages = new List<PropertyImage>();
        var displayOrder = existingCount > 0 ? property.PropertyImages.Max(pi => pi.DisplayOrder) + 1 : 1;
        foreach (var file in files)
        {
            var fileUrl = await _fileStorageService.SaveFileAsync(file, $"properties/{property.PropertyId}/images", ct);

            var isPrimary = !property.PropertyImages.Any(pi => pi.IsPrimary) && !newImages.Any();

            var propertyImage = new PropertyImage
            {
                PropertyId = property.PropertyId,
                Image = fileUrl,
                IsPrimary = isPrimary,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            property.PropertyImages.Add(propertyImage);
            newImages.Add(propertyImage);
        }

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        var uploadedIds = newImages.Select(pi => pi.ImageId).ToList();

        _logger.LogInformation("Uploaded {Count} images for property {PropertyId} by owner {OwnerId}", files.Count, id, ownerId);

        return uploadedIds;
    }

    public async Task DeleteImageAsync(Guid id, Guid imageId, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        var image = property.PropertyImages.FirstOrDefault(pi => pi.ImageId == imageId);
        if (image == null)
        {
            throw new NotFoundException("PropertyImage", imageId);
        }

        property.PropertyImages.Remove(image);
        await _fileStorageService.DeleteFileAsync(image.Image);

        if (image.IsPrimary && property.PropertyImages.Any())
        {
            var nextPrimary = property.PropertyImages.OrderBy(pi => pi.DisplayOrder).First();
            nextPrimary.IsPrimary = true;
        }

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted image {ImageId} for property {PropertyId} by owner {OwnerId}", imageId, id, ownerId);
    }
}
