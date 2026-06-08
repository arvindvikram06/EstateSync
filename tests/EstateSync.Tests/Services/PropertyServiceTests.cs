using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PropertyServiceTests
{
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IRepository<Location>> _locationRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IRepository<AgreementType>> _agreementTypeRepoMock;
    private PropertyMapper _propertyMapper;
    private Mock<ILogger<PropertyService>> _loggerMock;
    private PropertyService _propertyService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _locationRepoMock = new Mock<IRepository<Location>>();
        _userRepoMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _agreementTypeRepoMock = new Mock<IRepository<AgreementType>>();
        _propertyMapper = new PropertyMapper();
        _loggerMock = new Mock<ILogger<PropertyService>>();

        _propertyRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Property>()))
            .Callback<Property>(p => { if (p.PropertyId == Guid.Empty) p.PropertyId = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _propertyService = new PropertyService(
            _propertyRepoMock.Object,
            _locationRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _agreementTypeRepoMock.Object,
            _propertyMapper,
            _loggerMock.Object);
    }

    [Test]
    public void CreateAsync_ShouldThrowForbiddenException_WhenOwnerIsNotVerified()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var request = new CreatePropertyRequest
        {
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            LocationId = Guid.NewGuid()
        };

        _userRepoMock.Setup(x => x.IsUserVerifiedOwnerAsync(ownerId)).ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _propertyService.CreateAsync(request, ownerId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Owner must be verified before listing a property.");
        _propertyRepoMock.Verify(x => x.AddAsync(It.IsAny<Property>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ShouldCreatePropertyInDraftStatus_WhenOwnerIsVerified()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var request = new CreatePropertyRequest
        {
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            LocationId = locationId,
            MonthlyRent = 1500,
            Bedrooms = 2,
            Bathrooms = 2,
            PropertyType = PropertyType.Villa
        };

        var location = new Location
        {
            LocationId = locationId,
            StateName = "WA",
            District = "King",
            Pincode = "98101",
            Country = "USA"
        };

        _userRepoMock.Setup(x => x.IsUserVerifiedOwnerAsync(ownerId)).ReturnsAsync(true);
        _locationRepoMock.Setup(x => x.GetByIdAsync(locationId)).ReturnsAsync(location);

        // Act
        var result = await _propertyService.CreateAsync(request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        _propertyRepoMock.Verify(x => x.AddAsync(It.Is<Property>(p =>
            p.Title == request.Title &&
            p.OwnerId == ownerId &&
            p.Status == PropertyStatus.Draft
        )), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateAsync_ShouldThrowNotFoundException_WhenAgreementTypeDoesNotExist()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var agreementTypeId = Guid.NewGuid();
        var request = new CreatePropertyRequest
        {
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            LocationId = locationId,
            AgreementTypeId = agreementTypeId
        };

        var location = new Location
        {
            LocationId = locationId,
            StateName = "WA",
            District = "King",
            Pincode = "98101",
            Country = "USA"
        };

        _userRepoMock.Setup(x => x.IsUserVerifiedOwnerAsync(ownerId)).ReturnsAsync(true);
        _locationRepoMock.Setup(x => x.GetByIdAsync(locationId)).ReturnsAsync(location);
        _agreementTypeRepoMock.Setup(x => x.GetByIdAsync(agreementTypeId)).ReturnsAsync((AgreementType?)null);

        // Act
        Func<Task> act = async () => await _propertyService.CreateAsync(request, ownerId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"AgreementType with id {agreementTypeId} was not found.");
    }

    [Test]
    public void UpdateAsync_ShouldThrowNotFoundException_WhenAgreementTypeDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var agreementTypeId = Guid.NewGuid();
        var request = new UpdatePropertyRequest
        {
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            LocationId = locationId,
            AgreementTypeId = agreementTypeId
        };

        var property = new Property
        {
            PropertyId = id,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line"
        };
        var location = new Location
        {
            LocationId = locationId,
            StateName = "WA",
            District = "King",
            Pincode = "98101",
            Country = "USA"
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(id)).ReturnsAsync(property);
        _locationRepoMock.Setup(x => x.GetByIdAsync(locationId)).ReturnsAsync(location);
        _agreementTypeRepoMock.Setup(x => x.GetByIdAsync(agreementTypeId)).ReturnsAsync((AgreementType?)null);

        // Act
        Func<Task> act = async () => await _propertyService.UpdateAsync(id, request, ownerId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"AgreementType with id {agreementTypeId} was not found.");
    }
}
