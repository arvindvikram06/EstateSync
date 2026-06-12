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
    private Mock<ILeaseRepository> _leaseRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private PropertyMapper _propertyMapper;
    private Mock<ILogger<PropertyService>> _loggerMock;
    private PropertyService _propertyService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _locationRepoMock = new Mock<IRepository<Location>>();
        _userRepoMock = new Mock<IUserRepository>();
        _leaseRepoMock = new Mock<ILeaseRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
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
            _leaseRepoMock.Object,
            _unitOfWorkMock.Object,
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
        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Property 
            { 
                PropertyId = id, 
                Title = request.Title, 
                Description = request.Description,
                AddressLine = request.AddressLine,
                OwnerId = ownerId, 
                LocationId = locationId, 
                Location = location,
                Status = PropertyStatus.Draft,
                PropertyImages = new List<PropertyImage>()
            });

        // Act
        var result = await _propertyService.CreateAsync(request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PropertyId.Should().NotBeEmpty();
        result.Title.Should().Be(request.Title);
        result.OwnerId.Should().Be(ownerId);

        _propertyRepoMock.Verify(x => x.AddAsync(It.Is<Property>(p =>
            p.Title == request.Title &&
            p.OwnerId == ownerId &&
            p.Status == PropertyStatus.Draft
        )), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteAsync_ShouldThrowNotFoundException_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync((Property?)null);

        // Act
        Func<Task> act = async () => await _propertyService.DeleteAsync(propertyId, Guid.NewGuid(), false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public void DeleteAsync_ShouldThrowForbiddenException_WhenUserIsNotAdminAndDoesNotOwnProperty()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address"
        };
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        Func<Task> act = async () => await _propertyService.DeleteAsync(propertyId, callerId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public void DeleteAsync_ShouldThrowConflictException_WhenPropertyHasActiveLease()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address"
        };
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId))
            .ReturnsAsync(new Lease { LeaseId = Guid.NewGuid(), PropertyId = propertyId, Status = LeaseStatus.Active });

        // Act
        Func<Task> act = async () => await _propertyService.DeleteAsync(propertyId, ownerId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Cannot delete property because it has an active lease agreement.");
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteProperty_WhenUserIsOwnerAndNoActiveLease()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address"
        };
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId)).ReturnsAsync((Lease?)null);

        // Act
        await _propertyService.DeleteAsync(propertyId, ownerId, false, CancellationToken.None);

        // Assert
        _propertyRepoMock.Verify(x => x.DeleteAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteProperty_WhenUserIsAdminAndNoActiveLease()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address"
        };
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId)).ReturnsAsync((Lease?)null);

        // Act
        await _propertyService.DeleteAsync(propertyId, adminId, true, CancellationToken.None);

        // Assert
        _propertyRepoMock.Verify(x => x.DeleteAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
