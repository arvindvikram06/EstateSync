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
    private Mock<ILeaseRepository> _leaseRepoMock;
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
        _leaseRepoMock = new Mock<ILeaseRepository>();
        _loggerMock = new Mock<ILogger<RentalApplicationService>>();
        _mapper = new RentalApplicationMapper();

        _service = new RentalApplicationService(
            _rentalAppRepoMock.Object,
            _propertyRepoMock.Object,
            _userRepoMock.Object,
            _leaseRepoMock.Object,
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
            PhoneNumber = "123456789",
            IsIdentityVerified = true
        };

        var request = new CreateRentalApplicationRequest
        {
            Message = "I would love to rent this!",
            ExpectedMoveInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            DurationMonths = 12,
            CommuteDistance = 5.2m
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.HasTenantAppliedAsync(propertyId, tenantId)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);

        // Act
        var result = await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ApplicationId.Should().NotBeEmpty();
        result.PropertyId.Should().Be(propertyId);
        result.TenantId.Should().Be(tenantId);
        result.Message.Should().Be(request.Message);
        result.Status.Should().Be(ApplicationStatus.Pending.ToString());

        _rentalAppRepoMock.Verify(x => x.AddAsync(It.IsAny<RentalApplication>()), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == ownerId && n.Type == NotificationType.ApplicationReceived)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateApplicationAsync_ShouldThrowForbidden_WhenTenantNotIdentityVerified()
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
            PhoneNumber = "123456789",
            IsIdentityVerified = false
        };

        var request = new CreateRentalApplicationRequest
        {
            Message = "I would love to rent this!"
        };

        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);
        _rentalAppRepoMock.Setup(x => x.HasTenantAppliedAsync(propertyId, tenantId)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
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
        var tenant = new User { UserId = tenantId, IsIdentityVerified = true, FullName = "Tenant", Email = "t@t.com", PasswordHash = "hash", PhoneNumber = "123" };

        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);
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
        var tenant = new User { UserId = tenantId, IsIdentityVerified = true, FullName = "Tenant", Email = "t@t.com", PasswordHash = "hash", PhoneNumber = "123" };

        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);
        _propertyRepoMock.Setup(x => x.GetByIdAsync(propertyId)).ReturnsAsync(property);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("You can only apply to properties that are Active or Available Soon.");
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
        var tenant = new User { UserId = tenantId, IsIdentityVerified = true, FullName = "Tenant", Email = "t@t.com", PasswordHash = "hash", PhoneNumber = "123" };

        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);
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
        var tenant = new User { UserId = tenantId, IsIdentityVerified = true, FullName = "Tenant", Email = "t@t.com", PasswordHash = "hash", PhoneNumber = "123" };

        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(tenant);
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
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId))
            .ThrowsAsync(new NotFoundException("User", tenantId));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateApplicationAsync(propertyId, request, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ReviewApplicationAsync_ShouldApprove_AndKeepOthersPending()
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
        
        otherApp.Status.Should().Be(ApplicationStatus.Pending);
        otherApp.ReviewRemarks.Should().BeNull();

        // _rentalAppRepoMock.Verify(x => x.UpdateAsync(application), Times.Once);
        // _rentalAppRepoMock.Verify(x => x.UpdateAsync(otherApp), Times.Never);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == tenantId && n.Type == NotificationType.ApplicationApproved)), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == otherApp.TenantId && n.Type == NotificationType.ApplicationRejected)), Times.Never);
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

        // _rentalAppRepoMock.Verify(x => x.UpdateAsync(application), Times.Once);
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

        _rentalAppRepoMock.Setup(x => x.GetByOwnerIdPagedAsync(ownerId, 1, 20))
            .ReturnsAsync((new List<RentalApplication> { app }, 1));

        // Act
        var result = await _service.GetOwnerApplicationsAsync(ownerId, 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeEmpty().And.HaveCount(1);
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.Limit.Should().Be(20);
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
        _rentalAppRepoMock.Setup(x => x.GetByPropertyIdWithDetailsPagedAsync(propertyId, 1, 20))
            .ReturnsAsync((new List<RentalApplication> { app }, 1));

        // Act
        var result = await _service.GetApplicationsByPropertyIdAsync(propertyId, ownerId, 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeEmpty().And.HaveCount(1);
        result.Total.Should().Be(1);
    }

    [Test]
    public async Task GetTenantApplicationsAsync_ShouldReturnApplications_WhenTenantHasApplications()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            TenantId = tenantId,
            Message = "Test message",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetByTenantIdWithDetailsPagedAsync(tenantId, 1, 20))
            .ReturnsAsync((new List<RentalApplication> { app }, 1));

        // Act
        var result = await _service.GetTenantApplicationsAsync(tenantId, 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeEmpty().And.HaveCount(1);
        result.Total.Should().Be(1);
        result.Data.Should().ContainSingle(x => x.Message == "Test message");
    }

    [Test]
    public async Task GetApplicationByIdAsync_ShouldReturnApplication_WhenUserIsTenant()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication
        {
            ApplicationId = appId,
            TenantId = tenantId,
            Message = "Test message",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        // Act
        var result = await _service.GetApplicationByIdAsync(appId, tenantId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
    }

    [Test]
    public async Task GetApplicationByIdAsync_ShouldReturnApplication_WhenUserIsOwner()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Property Title",
            Description = "...",
            AddressLine = "..."
        };
        var app = new RentalApplication
        {
            ApplicationId = appId,
            TenantId = Guid.NewGuid(),
            PropertyId = property.PropertyId,
            Property = property,
            Message = "Test message",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        // Act
        var result = await _service.GetApplicationByIdAsync(appId, ownerId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetApplicationByIdAsync_ShouldReturnApplication_WhenUserIsAdmin()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var app = new RentalApplication
        {
            ApplicationId = appId,
            TenantId = Guid.NewGuid(),
            Message = "Test message",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        // Act
        var result = await _service.GetApplicationByIdAsync(appId, adminId, true, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetApplicationByIdAsync_ShouldThrowForbidden_WhenUserIsNotAuthorized()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(), // Different owner
            Title = "Property Title",
            Description = "...",
            AddressLine = "..."
        };
        var app = new RentalApplication
        {
            ApplicationId = appId,
            TenantId = Guid.NewGuid(), // Different tenant
            PropertyId = property.PropertyId,
            Property = property,
            Message = "Test message",
            Status = ApplicationStatus.Pending
        };

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        // Act & Assert
        Func<Task> act = async () => await _service.GetApplicationByIdAsync(appId, randomUserId, false, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetApplicationByIdAsync_ShouldThrowNotFound_WhenApplicationDoesNotExist()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _rentalAppRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync((RentalApplication?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.GetApplicationByIdAsync(appId, userId, false, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}



