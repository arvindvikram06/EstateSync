using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Complaints;

namespace EstateSync.Tests.Services;

[TestFixture]
public class ComplaintServiceTests
{
    private Mock<IComplaintRepository> _complaintRepoMock;
    private Mock<ILeaseRepository> _leaseRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IFileStorageService> _storageMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private ComplaintMapper _mapper;
    private ComplaintService _service;

    [SetUp]
    public void Setup()
    {
        _complaintRepoMock = new Mock<IComplaintRepository>();
        _leaseRepoMock = new Mock<ILeaseRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _storageMock = new Mock<IFileStorageService>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapper = new ComplaintMapper();

        _service = new ComplaintService(
            _complaintRepoMock.Object,
            _leaseRepoMock.Object,
            _userRepoMock.Object,
            _storageMock.Object,
            _notificationRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapper);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepoIsNull()
    {
        Action act = () => new ComplaintService(null!, _leaseRepoMock.Object, _userRepoMock.Object, _storageMock.Object, _notificationRepoMock.Object, _unitOfWorkMock.Object, _mapper);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- CreateComplaintAsync ---

    [Test]
    public async Task CreateComplaintAsync_ShouldThrowNotFound_WhenLeaseNotFound()
    {
        var request = new CreateComplaintRequest { LeaseId = Guid.NewGuid() };
        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(request.LeaseId)).ReturnsAsync((Lease?)null);

        Func<Task> act = () => _service.CreateComplaintAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Lease*");
    }

    [Test]
    public async Task CreateComplaintAsync_ShouldThrowForbidden_WhenTenantIdDoesNotMatch()
    {
        var request = new CreateComplaintRequest { LeaseId = Guid.NewGuid() };
        var lease = new Lease { LeaseId = request.LeaseId, TenantId = Guid.NewGuid() };
        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(request.LeaseId)).ReturnsAsync(lease);

        Func<Task> act = () => _service.CreateComplaintAsync(Guid.NewGuid(), request); // Different TenantId

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task CreateComplaintAsync_ShouldSucceed_AndSendNotificationToOwner()
    {
        var tenantId = Guid.NewGuid();
        var request = new CreateComplaintRequest { LeaseId = Guid.NewGuid(), Title = "Fix sink" };
        var lease = new Lease 
        { 
            LeaseId = request.LeaseId, 
            TenantId = tenantId, 
            Property = new Property { OwnerId = Guid.NewGuid(), Title = "Apt 1" } 
        };
        
        _leaseRepoMock.Setup(x => x.GetWithDetailsByIdAsync(request.LeaseId)).ReturnsAsync(lease);
        _complaintRepoMock.Setup(x => x.AddAsync(It.IsAny<Complaint>())).Returns(Task.CompletedTask);
        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Complaint { ComplaintId = Guid.NewGuid(), Title = "Fix sink" });

        var result = await _service.CreateComplaintAsync(tenantId, request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Fix sink");
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == lease.Property.OwnerId && n.Type == NotificationType.ComplaintRaised)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- UpdateComplaintStatusAsync ---

    [Test]
    public async Task UpdateComplaintStatusAsync_ShouldThrowNotFound_WhenComplaintNotFound()
    {
        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Complaint?)null);
        Func<Task> act = () => _service.UpdateComplaintStatusAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateComplaintStatusRequest());
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Complaint*");
    }

    [Test]
    public async Task UpdateComplaintStatusAsync_ShouldThrowNotFound_WhenUserNotFound()
    {
        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Complaint());
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        
        Func<Task> act = () => _service.UpdateComplaintStatusAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateComplaintStatusRequest());
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User*");
    }

    [Test]
    public async Task UpdateComplaintStatusAsync_ShouldThrowForbidden_WhenUnauthorized()
    {
        var complaint = new Complaint { Lease = new Lease { TenantId = Guid.NewGuid(), Property = new Property { OwnerId = Guid.NewGuid() } } };
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole>() }; // Neither tenant, owner, nor admin

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(complaint);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);

        Func<Task> act = () => _service.UpdateComplaintStatusAsync(user.UserId, Guid.NewGuid(), new UpdateComplaintStatusRequest());
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task UpdateComplaintStatusAsync_ShouldThrowBadRequest_WhenTenantUpdatesToNonClosed()
    {
        var tenantId = Guid.NewGuid();
        var complaint = new Complaint { Lease = new Lease { TenantId = tenantId } };
        var user = new User { UserId = tenantId, UserRoles = new List<UserRole>() }; 

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(complaint);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(tenantId)).ReturnsAsync(user);

        Func<Task> act = () => _service.UpdateComplaintStatusAsync(tenantId, Guid.NewGuid(), new UpdateComplaintStatusRequest { Status = ComplaintStatus.Resolved });
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task UpdateComplaintStatusAsync_ShouldSucceed_AndNotifyTenant_WhenOwnerUpdatesToResolved()
    {
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var complaint = new Complaint { ComplaintId = Guid.NewGuid(), Title = "Issue", Lease = new Lease { TenantId = tenantId, Property = new Property { OwnerId = ownerId } } };
        var user = new User { UserId = ownerId, UserRoles = new List<UserRole>() }; // Owner

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(complaint.ComplaintId)).ReturnsAsync(complaint);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(ownerId)).ReturnsAsync(user);

        var request = new UpdateComplaintStatusRequest { Status = ComplaintStatus.Resolved, ReviewRemarks = "Fixed it" };
        var result = await _service.UpdateComplaintStatusAsync(ownerId, complaint.ComplaintId, request);

        result.Status.Should().Be(ComplaintStatus.Resolved.ToString());
        complaint.ReviewRemarks.Should().Be("Fixed it");
        
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == tenantId && n.Type == NotificationType.ComplaintResolved)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- GetByIdAsync ---

    [Test]
    public async Task GetByIdAsync_ShouldThrowForbidden_WhenUnauthorizedUser()
    {
        var complaint = new Complaint { Lease = new Lease { TenantId = Guid.NewGuid(), Property = new Property { OwnerId = Guid.NewGuid() } } };
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole>() }; 

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(complaint);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);

        Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid(), user.UserId);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetByIdAsync_ShouldSucceed_WhenAdmin()
    {
        var complaint = new Complaint { ComplaintId = Guid.NewGuid() };
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole> { new UserRole { Role = new Role { Name = "Admin" } } } }; 

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(complaint.ComplaintId)).ReturnsAsync(complaint);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);

        var result = await _service.GetByIdAsync(complaint.ComplaintId, user.UserId);
        result.ComplaintId.Should().Be(complaint.ComplaintId);
    }

    // --- GetMyComplaintsAsync ---

    [Test]
    public async Task GetMyComplaintsAsync_ShouldReturnAdminComplaints_WhenAdmin()
    {
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole> { new UserRole { Role = new Role { Name = "Admin" } } } };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);
        _complaintRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Complaint> { new Complaint(), new Complaint() });

        var results = await _service.GetMyComplaintsAsync(user.UserId);
        results.Count().Should().Be(2);
        _complaintRepoMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetMyComplaintsAsync_ShouldReturnOwnerComplaints_WhenOwner()
    {
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole> { new UserRole { Role = new Role { Name = "Owner" } } } };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);
        _complaintRepoMock.Setup(x => x.GetByOwnerIdAsync(user.UserId)).ReturnsAsync(new List<Complaint> { new Complaint() });

        var results = await _service.GetMyComplaintsAsync(user.UserId);
        results.Count().Should().Be(1);
        _complaintRepoMock.Verify(x => x.GetByOwnerIdAsync(user.UserId), Times.Once);
    }

    [Test]
    public async Task GetMyComplaintsAsync_ShouldReturnTenantComplaints_WhenTenant()
    {
        var user = new User { UserId = Guid.NewGuid(), UserRoles = new List<UserRole>() }; // Neither Admin nor Owner => Tenant
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(user.UserId)).ReturnsAsync(user);
        _complaintRepoMock.Setup(x => x.GetByTenantIdAsync(user.UserId)).ReturnsAsync(new List<Complaint> { new Complaint(), new Complaint(), new Complaint() });

        var results = await _service.GetMyComplaintsAsync(user.UserId);
        results.Count().Should().Be(3);
        _complaintRepoMock.Verify(x => x.GetByTenantIdAsync(user.UserId), Times.Once);
    }

    // --- UploadComplaintImageAsync ---

    [Test]
    public async Task UploadComplaintImageAsync_ShouldThrowForbidden_WhenNotTenant()
    {
        var tenantId = Guid.NewGuid();
        var complaint = new Complaint { ComplaintId = Guid.NewGuid(), Lease = new Lease { TenantId = Guid.NewGuid() } };
        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(complaint.ComplaintId)).ReturnsAsync(complaint);
        
        Func<Task> act = () => _service.UploadComplaintImageAsync(tenantId, complaint.ComplaintId, new Mock<IFormFile>().Object);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task UploadComplaintImageAsync_ShouldThrowBadRequest_WhenFileNull()
    {
        var tenantId = Guid.NewGuid();
        var complaint = new Complaint { ComplaintId = Guid.NewGuid(), Lease = new Lease { TenantId = tenantId } };
        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(complaint.ComplaintId)).ReturnsAsync(complaint);
        
        Func<Task> act = () => _service.UploadComplaintImageAsync(tenantId, complaint.ComplaintId, null!);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task UploadComplaintImageAsync_ShouldSucceed()
    {
        var tenantId = Guid.NewGuid();
        var complaint = new Complaint { ComplaintId = Guid.NewGuid(), Lease = new Lease { TenantId = tenantId }, ComplaintImages = new List<ComplaintImage>() };
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(x => x.Length).Returns(100);

        _complaintRepoMock.Setup(x => x.GetWithDetailsByIdAsync(complaint.ComplaintId)).ReturnsAsync(complaint);
        _storageMock.Setup(x => x.SaveFileAsync(fileMock.Object, "complaints", It.IsAny<CancellationToken>())).ReturnsAsync("/path/to/img.png");

        var result = await _service.UploadComplaintImageAsync(tenantId, complaint.ComplaintId, fileMock.Object);

        result.Should().NotBeNull();
        complaint.ComplaintImages.Count.Should().Be(1);
        complaint.ComplaintImages.First().Image.Should().Be("/path/to/img.png");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
