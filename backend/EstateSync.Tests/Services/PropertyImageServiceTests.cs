using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Storage;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.Shared;
using EstateSync.Api.Common;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PropertyImageServiceTests
{
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<PropertyImageService>> _loggerMock;
    private Mock<ICacheService> _cacheMock;
    private PropertyImageService _propertyImageService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PropertyImageService>>();
        _cacheMock = new Mock<ICacheService>();

        _propertyImageService = new PropertyImageService(
            _propertyRepoMock.Object,
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object);
    }

    [Test]
    public async Task UploadImagesAsync_ShouldAddImagesAndSetPrimary()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Property Title",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            PropertyImages = new List<PropertyImage>()
        };

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("image1.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(2048);

        var files = new List<IFormFile> { mockFile.Object };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(mockFile.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/properties/image1.jpg");

        // Act
        var result = await _propertyImageService.UploadImagesAsync(propertyId, files, ownerId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        property.PropertyImages.Should().HaveCount(1);
        property.PropertyImages.First().IsPrimary.Should().BeTrue();
        property.PropertyImages.First().Image.Should().Be("/uploads/properties/image1.jpg");

        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(x => x.IncrementAsync(
            PropertyCacheKeys.ListingsVersion,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
