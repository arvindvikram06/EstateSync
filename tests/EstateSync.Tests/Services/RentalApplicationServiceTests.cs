using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.RentalApplications;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.RentalApplications;

namespace EstateSync.Tests.Services;

[TestFixture]
public class RentalApplicationServiceTests
{
    private Mock<IRentalApplicationRepository> _rentalAppRepoMock;
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<RentalApplicationService>> _loggerMock;
    private RentalApplicationMapper _mapper;
    private RentalApplicationService _service;

    [SetUp]
    public void SetUp()
    {
        _rentalAppRepoMock = new Mock<IRentalApplicationRepository>();
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RentalApplicationService>>();
        _mapper = new RentalApplicationMapper();

        _service = new RentalApplicationService(
            _rentalAppRepoMock.Object,
            _propertyRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldSucceed_WhenValidRequest()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Beautiful Villa",
            Description = "Great place",
            AddressLine = "123 Main St",
            Status = PropertyStatus.Active
        };

        var tenant = new User
        {
            UserId = tenantId,
            FullName = "John Tenant",
            Email = "john@tenant.com",
            PasswordHash = "hash",
            PhoneNumber = "123456789"
        };

        var request = new CreateRentalApplicationRequest
        {
            Message = "I would love to rent this!",
            MoveInDate = DateTime.Today.AddDays(7),
            MoveOutDate = DateTime.Today.AddYears(1),
            NegotiatedPrice = 1500,
            CommuteDistance = 5.2m
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.HasTenantAppliedAsync(propertyId, tenantId)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);

        // Act
        var result = await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PropertyId.Should().Be(propertyId);
        result.TenantId.Should().Be(tenantId);
        result.Message.Should().Be(request.Message);
        result.Status.Should().Be(ApplicationStatus.Pending.ToString());

        _rentalAppRepoMock.Verify(x => x.AddAsync(It.IsAny<RentalApplication>()), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == ownerId && n.Type == NotificationType.ApplicationReceived)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new CreateRentalApplicationRequest
        {
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync((Property?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowBadRequest_WhenPropertyIsNotActive()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = Guid.NewGuid(),
            Title = "Draft Property",
            Description = "...",
            AddressLine = "...",
            Status = PropertyStatus.Draft
        };
        var request = new CreateRentalApplicationRequest
        {
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("You can only apply to Active properties.");
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowBadRequest_WhenTenantIsOwner()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = tenantId, // Tenant is the owner
            Title = "My Own Property",
            Description = "...",
            AddressLine = "...",
            Status = PropertyStatus.Active
        };
        var request = new CreateRentalApplicationRequest
        {
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("You cannot apply to your own property.");
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowConflict_WhenTenantHasAlreadyApplied()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = Guid.NewGuid(),
            Title = "Some Property",
            Description = "...",
            AddressLine = "...",
            Status = PropertyStatus.Active
        };
        var request = new CreateRentalApplicationRequest
        {
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.HasTenantAppliedAsync(propertyId, tenantId)).ReturnsAsync(true);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("You have already applied to this property.");
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = Guid.NewGuid(),
            Title = "Some Property",
            Description = "...",
            AddressLine = "...",
            Status = PropertyStatus.Active
        };
        var request = new CreateRentalApplicationRequest
        {
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.HasTenantAppliedAsync(propertyId, tenantId)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync((User?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ReviewApplicationAsync_ShouldApprove_AndRejectOthers()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Beautiful Villa",
            Description = "Great place",
            AddressLine = "123 Main St",
            Status = PropertyStatus.Active
        };

        var application = new RentalApplication
        {
            ApplicationId = appId,
            PropertyId = propertyId,
            Property = property,
            TenantId = tenantId,
            Message = "App 1",
            Status = ApplicationStatus.Pending
        };

        var otherApp = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = Guid.NewGuid(),
            Message = "App 2",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(application);
        _rentalAppRepoMock.Setup(x => x.GetByPropertyIdAsync(propertyId)).ReturnsAsync(new List<RentalApplication> { application, otherApp });

        // Act
        var result = await _service.ReviewApplicationAsync(appId, true, "Approved!", ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApplicationStatus.Approved.ToString());
        result.ReviewRemarks.Should().Be("Approved!");
        
        otherApp.Status.Should().Be(ApplicationStatus.Rejected);
        otherApp.ReviewRemarks.Should().Be("Another application was approved for this property.");

        _rentalAppRepoMock.Verify(x => x.UpdateAsync(application), Times.Once);
        _rentalAppRepoMock.Verify(x => x.UpdateAsync(otherApp), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == tenantId && n.Type == NotificationType.ApplicationApproved)), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == otherApp.TenantId && n.Type == NotificationType.ApplicationRejected)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReviewApplicationAsync_ShouldReject_WhenApproveIsFalse()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Beautiful Villa",
            Description = "Great place",
            AddressLine = "123 Main St",
            Status = PropertyStatus.Active
        };

        var application = new RentalApplication
        {
            ApplicationId = appId,
            PropertyId = propertyId,
            Property = property,
            TenantId = tenantId,
            Message = "App 1",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(application);

        // Act
        var result = await _service.ReviewApplicationAsync(appId, false, "Not a good fit", ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApplicationStatus.Rejected.ToString());
        result.ReviewRemarks.Should().Be("Not a good fit");

        _rentalAppRepoMock.Verify(x => x.UpdateAsync(application), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == tenantId && n.Type == NotificationType.ApplicationRejected)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReviewApplicationAsync_ShouldThrowForbidden_WhenOwnerDoesNotOwnProperty()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(), // Different owner
            Title = "Beautiful Villa",
            Description = "...",
            AddressLine = "...",
            Status = PropertyStatus.Active
        };

        var application = new RentalApplication
        {
            ApplicationId = appId,
            Property = property,
            Status = ApplicationStatus.Pending,
            Message = "..."
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(application);

        // Act & Assert
        Func<Task> act = async () => await _service.ReviewApplicationAsync(appId, true, "Ok", ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetOwnerApplicationsAsync_ShouldReturnApplications()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var app = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            Message = "Interested",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetByOwnerIdAsync(ownerId)).ReturnsAsync(new List<RentalApplication> { app });

        // Act
        var result = await _service.GetOwnerApplicationsAsync(ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty().And.HaveCount(1);
    }

    [Test]
    public async Task GetApplicationsByPropertyIdAsync_ShouldReturnApplications_WhenOwnerOwnsProperty()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "My Villa",
            Description = "...",
            AddressLine = "..."
        };

        var app = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            Property = property,
            Message = "Test message"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.GetByPropertyIdWithDetailsAsync(propertyId)).ReturnsAsync(new List<RentalApplication> { app });

        // Act
        var result = await _service.GetApplicationsByPropertyIdAsync(propertyId, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty().And.HaveCount(1);
    }
}
