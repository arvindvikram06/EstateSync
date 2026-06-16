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
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Leases;

namespace EstateSync.Tests.Services;

[TestFixture]
public class LeaseServiceTests
{
    private Mock<ILeaseRepository> _leaseRepoMock;
    private Mock<IRentalApplicationRepository> _applicationRepoMock;
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<ILeaseBillRepository> _leaseBillRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<LeaseService>> _loggerMock;
    private LeaseMapper _mapper;
    private LeaseService _service;

    [SetUp]
    public void SetUp()
    {
        _leaseRepoMock = new Mock<ILeaseRepository>();
        _applicationRepoMock = new Mock<IRentalApplicationRepository>();
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _leaseBillRepoMock = new Mock<ILeaseBillRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LeaseService>>();
        _mapper = new LeaseMapper();

        _service = new LeaseService(
            _leaseRepoMock.Object,
            _applicationRepoMock.Object,
            _propertyRepoMock.Object,
            _fileStorageServiceMock.Object,
            _notificationRepoMock.Object,
            _leaseBillRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private Property CreateTestProperty(Guid propertyId, Guid ownerId, PropertyStatus status = PropertyStatus.Active)
    {
        return new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            Status = status
        };
    }

    private RentalApplication CreateTestApplication(
        Guid applicationId,
        Guid propertyId,
        Guid tenantId,
        Guid ownerId,
        ApplicationStatus status = ApplicationStatus.Approved,
        bool hasApprovedVerification = true)
    {
        var app = new RentalApplication
        {
            ApplicationId = applicationId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Message = "Test application message",
            Status = status,
            Property = CreateTestProperty(propertyId, ownerId)
        };

        if (hasApprovedVerification)
        {
            app.TenantVerificationRequests = new List<TenantVerificationRequest>
            {
                new TenantVerificationRequest
                {
                    RequestId = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    TenantId = tenantId,
                    Status = ApprovalStatus.Approved
                }
            };
        }

        return app;
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldSucceed_WhenValidRequest()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var app = CreateTestApplication(applicationId, propertyId, tenantId, ownerId, ApplicationStatus.Approved);

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly,
            AgreementText = "Lease agreement terms..."
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync(app);
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId)).ReturnsAsync((Lease?)null);

        // Act
        var result = await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LeaseId.Should().NotBeEmpty();
        result.MonthlyRent.Should().Be(2000);
        result.SecurityDeposit.Should().Be(4000);
        result.AgreementText.Should().Be(request.AgreementText);
        result.Status.Should().Be(LeaseStatus.PendingApproval.ToString());

        _leaseRepoMock.Verify(x => x.AddAsync(It.IsAny<Lease>()), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n =>
            n.UserId == tenantId &&
            n.Type == NotificationType.LeaseCreated &&
            n.EntityType == "Lease"
        )), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync((RentalApplication?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldThrowBadRequestException_WhenApplicationIsNotApproved()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var app = CreateTestApplication(applicationId, Guid.NewGuid(), Guid.NewGuid(), ownerId, ApplicationStatus.Pending);

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync(app);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Rental application is not approved.");
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldThrowBadRequestException_WhenTenantVerificationIsNotApproved()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var app = CreateTestApplication(applicationId, propertyId, Guid.NewGuid(), ownerId, ApplicationStatus.Approved, hasApprovedVerification: false);

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync(app);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tenant verification request has not been approved yet.");
    }


    [Test]
    public async Task CreateLeaseAsync_ShouldThrowForbiddenException_WhenOwnerDoesNotOwnProperty()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var app = CreateTestApplication(applicationId, propertyId, Guid.NewGuid(), ownerId, ApplicationStatus.Approved);

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync(app);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, strangerId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("You do not own this property.");
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldThrowConflictException_WhenActiveLeaseAlreadyExists()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var app = CreateTestApplication(applicationId, propertyId, Guid.NewGuid(), ownerId, ApplicationStatus.Approved);

        var request = new CreateLeaseRequest
        {
            ApplicationId = applicationId,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddYears(1),
            MonthlyRent = 2000,
            SecurityDeposit = 4000,
            PaymentFrequency = PaymentFrequency.Monthly
        };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(applicationId)).ReturnsAsync(app);
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId)).ReturnsAsync(new Lease());

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("An active lease already exists for this property.");
    }

    [Test]
    public async Task SignLeaseAsync_ShouldSucceed_WhenTenantSigns()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Status = LeaseStatus.PendingApproval,
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("agreement.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(500);

        var request = new SignLeaseRequest
        {
            SignatureText = "John Tenant Signature",
            SignedAgreementFile = fileMock.Object
        };

        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://storage/agreement.pdf");

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, tenantId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantSignature.Should().Be("John Tenant Signature");
        result.TenantSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PendingApproval.ToString()); // owner hasn't signed yet

        // _leaseRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Lease>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignLeaseAsync_ShouldSucceed_WhenOwnerSigns()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Status = LeaseStatus.PendingApproval,
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Jane Owner Signature"
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerSignature.Should().Be("Jane Owner Signature");
        result.OwnerSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PendingApproval.ToString()); // tenant hasn't signed yet

        // _leaseRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Lease>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignLeaseAsync_ShouldAutoActivate_WhenBothPartiesHaveSigned()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Status = LeaseStatus.PendingApproval,
            TenantSignedAt = DateTime.UtcNow,
            TenantSignature = "Tenant Signature",
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Owner Signature"
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerSignature.Should().Be("Owner Signature");
        result.OwnerSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.Active.ToString());
        result.ActivatedAt.Should().NotBeNull();
        lease.Property.Status.Should().Be(PropertyStatus.Leased);

        // _propertyRepoMock.Verify(x => x.UpdateAsync(lease.Property), Times.Once);
        // _leaseRepoMock.Verify(x => x.UpdateAsync(lease), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignLeaseAsync_ShouldThrowForbiddenException_WhenStrangerSigns()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Status = LeaseStatus.PendingApproval,
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Stranger Signature"
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act & Assert
        Func<Task> act = async () => await _service.SignLeaseAsync(leaseId, request, strangerId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("You are not authorized to sign this lease.");
    }

    [Test]
    public async Task GetLeaseByIdAsync_ShouldSucceed_WhenTenantViews()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            TenantId = tenantId,
            Property = CreateTestProperty(Guid.NewGuid(), Guid.NewGuid())
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.GetLeaseByIdAsync(leaseId, tenantId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetLeaseByIdAsync_ShouldThrowForbiddenException_WhenStrangerViews()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            TenantId = tenantId,
            Property = CreateTestProperty(Guid.NewGuid(), Guid.NewGuid())
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act & Assert
        Func<Task> act = async () => await _service.GetLeaseByIdAsync(leaseId, strangerId, false, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task SignLeaseAsync_ShouldCancelOtherApplications_WhenActivated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentAppId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            ApplicationId = currentAppId,
            Status = LeaseStatus.PendingApproval,
            TenantSignedAt = DateTime.UtcNow,
            TenantSignature = "Tenant Signature",
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var otherApp = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = Guid.NewGuid(),
            Status = ApplicationStatus.Pending,
            Message = "App 2"
        };

        var approvedApp = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = Guid.NewGuid(),
            Status = ApplicationStatus.Approved,
            Message = "App 3"
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Owner Signature"
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);
        _applicationRepoMock.Setup(x => x.GetByPropertyIdAsync(propertyId))
            .ReturnsAsync(new List<RentalApplication> { otherApp, approvedApp });

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.Active.ToString());

        otherApp.Status.Should().Be(ApplicationStatus.Rejected);
        otherApp.ReviewRemarks.Should().Be("Property is reserved");

        approvedApp.Status.Should().Be(ApplicationStatus.Rejected);
        approvedApp.ReviewRemarks.Should().Be("Property is reserved");

        // _applicationRepoMock.Verify(x => x.UpdateAsync(otherApp), Times.Once);
        // _applicationRepoMock.Verify(x => x.UpdateAsync(approvedApp), Times.Once);
        
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => 
            n.UserId == otherApp.TenantId && 
            n.Type == NotificationType.ApplicationRejected &&
            n.Message.Contains("Property is reserved")
        )), Times.Once);
    }
}
