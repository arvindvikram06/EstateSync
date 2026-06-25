using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PropertyAmenityServiceTests
{
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<PropertyAmenityService>> _loggerMock;
    private PropertyAmenityService _propertyAmenityService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PropertyAmenityService>>();

        _propertyAmenityService = new PropertyAmenityService(
            _propertyRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task AddAmenitiesAsync_ShouldAppendAmenitiesToProperty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = id,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            Amenities = new List<string> { "wifi" }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(id)).ReturnsAsync(property);

        // Act
        await _propertyAmenityService.AddAmenitiesAsync(id, new List<string> { "Swimming Pool", "wifi" }, ownerId, CancellationToken.None);

        // Assert
        property.Amenities.Should().BeEquivalentTo(new List<string> { "wifi", "swimmingpool" });
        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAmenitiesAsync_ShouldReplaceAmenitiesOfProperty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = id,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            Amenities = new List<string> { "wifi", "gym" }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(id)).ReturnsAsync(property);

        // Act
        await _propertyAmenityService.UpdateAmenitiesAsync(id, new List<string> { "Swimming Pool", "AC" }, ownerId, CancellationToken.None);

        // Assert
        property.Amenities.Should().BeEquivalentTo(new List<string> { "swimmingpool", "ac" });
        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAmenitiesAsync_ShouldRemoveAmenitiesFromProperty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = id,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            Amenities = new List<string> { "wifi", "swimmingpool", "ac" }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(id)).ReturnsAsync(property);

        // Act
        await _propertyAmenityService.DeleteAmenitiesAsync(id, new List<string> { "Swimming Pool", "ac" }, ownerId, CancellationToken.None);

        // Assert
        property.Amenities.Should().BeEquivalentTo(new List<string> { "wifi" });
        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
