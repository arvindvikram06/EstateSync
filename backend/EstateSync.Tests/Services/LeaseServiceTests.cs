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
using EstateSync.Api.Interfaces.Services.Leases;
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
    private Mock<ILeaseDocumentService> _leaseDocumentServiceMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IUserRepository> _userRepoMock;
    private LeaseMapper _mapper;
    private LeaseService _service;

    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

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
        _leaseDocumentServiceMock = new Mock<ILeaseDocumentService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _userRepoMock = new Mock<IUserRepository>();
        _mapper = new LeaseMapper();

        _leaseDocumentServiceMock
            .Setup(x => x.GenerateAgreementPdfAsync(It.IsAny<Lease>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/test/agreement.pdf");
        _leaseDocumentServiceMock
            .Setup(x => x.GenerateSignedAgreementPdfAsync(It.IsAny<Lease>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/test/signed-agreement.pdf");
        _fileStorageServiceMock
            .Setup(x => x.SaveBytesAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/test/sig.png");
        _userRepoMock
            .Setup(x => x.GetUsersByRoleAsync("Admin"))
            .ReturnsAsync(new List<User>());

        _service = new LeaseService(
            _leaseRepoMock.Object,
            _applicationRepoMock.Object,
            _propertyRepoMock.Object,
            _fileStorageServiceMock.Object,
            _notificationRepoMock.Object,
            _leaseBillRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object,
            _leaseDocumentServiceMock.Object,
            _httpContextAccessorMock.Object,
            _userRepoMock.Object);
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
        bool tenantKycVerified = true)
    {
        return new RentalApplication
        {
            ApplicationId = applicationId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Message = "Test application message",
            Status = status,
            Property = CreateTestProperty(propertyId, ownerId),
            Tenant = new User
            {
                UserId = tenantId,
                FullName = "Test Tenant",
                Email = "tenant@test.com",
                PhoneNumber = "123456789",
                PasswordHash = "hash",
                IsKycVerified = tenantKycVerified
            }
        };
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

        var app = CreateTestApplication(applicationId, propertyId, Guid.NewGuid(), ownerId, ApplicationStatus.Approved, tenantKycVerified: false);

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
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tenant has not completed KYC verification.");
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
        _leaseRepoMock.Setup(x => x.GetActiveLeaseByPropertyIdAsync(propertyId)).ReturnsAsync(new Lease { EndDate = DateTime.Today.AddYears(2) });

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("The new lease must start after the current active lease ends.");
    }

    [Test]
    public async Task CreateLeaseAsync_ShouldThrowConflictException_WhenLeaseForApplicationAlreadyExists()
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
        _leaseRepoMock.Setup(x => x.GetByApplicationIdAsync(applicationId)).ReturnsAsync(new Lease { Status = LeaseStatus.PendingApproval });

        // Act & Assert
        Func<Task> act = async () => await _service.CreateLeaseAsync(request, ownerId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("A lease for this rental application is already pending approval.");
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

        var request = new SignLeaseRequest
        {
            SignatureText = "John Tenant Signature",
            SignatureImageBase64 = ValidBase64Png
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, tenantId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantSignature.Should().Be("John Tenant Signature");
        result.TenantSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PartialSigned.ToString()); // owner hasn't signed yet

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
            SignatureText = "Jane Owner Signature",
            SignatureImageBase64 = ValidBase64Png
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerSignature.Should().Be("Jane Owner Signature");
        result.OwnerSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PartialSigned.ToString()); // tenant hasn't signed yet

        // _leaseRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Lease>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignLeaseAsync_ShouldMoveToPendingAdminApproval_WhenBothPartiesHaveSigned()
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
            Status = LeaseStatus.PartialSigned,
            TenantSignedAt = DateTime.UtcNow,
            TenantSignature = "Tenant Signature",
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Owner Signature",
            SignatureImageBase64 = ValidBase64Png
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerSignature.Should().Be("Owner Signature");
        result.OwnerSignedAt.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PendingAdminApproval.ToString());
        result.SignatureVerificationHash.Should().NotBeNullOrEmpty();

        _leaseDocumentServiceMock.Verify(x => x.GenerateSignedAgreementPdfAsync(lease, It.IsAny<CancellationToken>()), Times.Once);
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
            SignatureText = "Stranger Signature",
            SignatureImageBase64 = ValidBase64Png
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
    public async Task SignLeaseAsync_ShouldNotCancelApplications_WhenBothPartiesSign()
    {
        // Application rejection happens at admin approval, not at signing.
        // When both parties sign the lease moves to PendingAdminApproval.
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
            Status = LeaseStatus.PartialSigned,
            TenantSignedAt = DateTime.UtcNow,
            TenantSignature = "Tenant Signature",
            Property = CreateTestProperty(propertyId, ownerId)
        };

        var request = new SignLeaseRequest
        {
            SignatureText = "Owner Signature",
            SignatureImageBase64 = ValidBase64Png
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Act
        var result = await _service.SignLeaseAsync(leaseId, request, ownerId, CancellationToken.None);

        // Assert — both signed → PendingAdminApproval; applications untouched until admin approves
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaseStatus.PendingAdminApproval.ToString());
        _applicationRepoMock.Verify(x => x.GetByPropertyIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ─── TerminateLeaseAsync ─────────────────────────────────────────────────

    [Test]
    public async Task TerminateLeaseAsync_ActiveLease_SetsTerminatedAndForfeitsDeposit()
    {
        var leaseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = CreateTestProperty(propertyId, ownerId, PropertyStatus.Leased);

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            Status = LeaseStatus.Active,
            Property = property,
            SecurityDeposit = 60000m
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        await _service.TerminateLeaseAsync(leaseId, ownerId, false, "Breaking lease early", CancellationToken.None);

        lease.Status.Should().Be(LeaseStatus.Terminated);
        lease.IsDepositForfeited.Should().BeTrue();
        lease.CancelledBy.Should().Be(ownerId);
        lease.CancellationReason.Should().Be("Breaking lease early");
        property.Status.Should().Be(PropertyStatus.Active);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.IsAny<Notification>()), Times.Exactly(2));
    }

    [Test]
    public async Task TerminateLeaseAsync_AdminCanTerminateAnyLease()
    {
        var leaseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = CreateTestProperty(Guid.NewGuid(), ownerId, PropertyStatus.Leased);

        var lease = new Lease
        {
            LeaseId = leaseId,
            TenantId = tenantId,
            Status = LeaseStatus.Active,
            Property = property
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        // Admin is not the owner — should still succeed because isAdmin = true
        await _service.TerminateLeaseAsync(leaseId, adminId, true, "Admin termination reason", CancellationToken.None);

        lease.Status.Should().Be(LeaseStatus.Terminated);
        lease.IsDepositForfeited.Should().BeTrue();
    }

    [Test]
    public async Task TerminateLeaseAsync_NonOwnerNonAdmin_ThrowsForbidden()
    {
        var leaseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var property = CreateTestProperty(Guid.NewGuid(), ownerId);

        var lease = new Lease
        {
            LeaseId = leaseId,
            Status = LeaseStatus.Active,
            Property = property
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        var act = async () => await _service.TerminateLeaseAsync(leaseId, randomUserId, false, "Some reason", CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task TerminateLeaseAsync_NonActiveLease_ThrowsBadRequest()
    {
        var leaseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = CreateTestProperty(Guid.NewGuid(), ownerId);

        var lease = new Lease
        {
            LeaseId = leaseId,
            Status = LeaseStatus.Expired,
            Property = property
        };

        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync(lease);

        var act = async () => await _service.TerminateLeaseAsync(leaseId, ownerId, false, "Some reason", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task TerminateLeaseAsync_LeaseNotFound_ThrowsNotFoundException()
    {
        var leaseId = Guid.NewGuid();
        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(leaseId)).ReturnsAsync((Lease?)null);

        var act = async () => await _service.TerminateLeaseAsync(leaseId, Guid.NewGuid(), false, "reason here", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
