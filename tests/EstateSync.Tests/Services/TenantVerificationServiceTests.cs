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
using EstateSync.Api.DTOs.TenantVerification;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.TenantVerification;

namespace EstateSync.Tests.Services;

[TestFixture]
public class TenantVerificationServiceTests
{
    private Mock<ITenantVerificationRequestRepository> _requestRepoMock;
    private Mock<IRentalApplicationRepository> _applicationRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private TenantVerificationMapper _mapper;
    private Mock<ILogger<TenantVerificationService>> _loggerMock;
    private TenantVerificationService _service;

    [SetUp]
    public void SetUp()
    {
        _requestRepoMock = new Mock<ITenantVerificationRequestRepository>();
        _applicationRepoMock = new Mock<IRentalApplicationRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapper = new TenantVerificationMapper(new UserMapper());
        _loggerMock = new Mock<ILogger<TenantVerificationService>>();

        _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<TenantVerificationRequest>()))
            .Callback<TenantVerificationRequest>(r => { if (r.RequestId == Guid.Empty) r.RequestId = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _documentRepoMock.Setup(d => d.AddAsync(It.IsAny<Document>()))
            .Callback<Document>(d => { if (d.DocumentId == Guid.Empty) d.DocumentId = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _service = new TenantVerificationService(
            _requestRepoMock.Object,
            _applicationRepoMock.Object,
            _documentRepoMock.Object,
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync((RentalApplication?)null);

        Func<Task> act = () => _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldThrowForbiddenException_WhenTenantIdDoesNotMatch()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication { ApplicationId = appId, TenantId = Guid.NewGuid(), Message = "test" };
        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        Func<Task> act = () => _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldThrowBadRequestException_WhenApplicationStatusIsNotApproved()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication { ApplicationId = appId, TenantId = tenantId, Status = ApplicationStatus.Pending, Message = "test" };
        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);

        Func<Task> act = () => _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldThrowConflictException_WhenActiveRequestAlreadyExists()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication { ApplicationId = appId, TenantId = tenantId, Status = ApplicationStatus.Approved, Message = "test" };
        var request = new TenantVerificationRequest { RequestId = Guid.NewGuid(), ApplicationId = appId, TenantId = tenantId, Status = ApprovalStatus.Pending };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);
        _requestRepoMock.Setup(x => x.GetByApplicationIdAsync(appId)).ReturnsAsync(request);

        Func<Task> act = () => _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldReturnExistingDraft_WhenDraftExists()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication { ApplicationId = appId, TenantId = tenantId, Status = ApplicationStatus.Approved, Message = "test" };
        var request = new TenantVerificationRequest { RequestId = Guid.NewGuid(), ApplicationId = appId, TenantId = tenantId, Status = ApprovalStatus.Draft };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);
        _requestRepoMock.Setup(x => x.GetByApplicationIdAsync(appId)).ReturnsAsync(request);
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(request.RequestId)).ReturnsAsync(request);

        var result = await _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result.RequestId.Should().Be(request.RequestId);
        result.Status.Should().Be("Draft");
    }

    [Test]
    public async Task InitiateVerificationAsync_ShouldCreateNewDraft_WhenNoRequestExists()
    {
        var appId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var app = new RentalApplication { ApplicationId = appId, TenantId = tenantId, Status = ApplicationStatus.Approved, Message = "test" };

        _applicationRepoMock.Setup(x => x.GetWithDetailsByIdAsync(appId)).ReturnsAsync(app);
        _requestRepoMock.Setup(x => x.GetByApplicationIdAsync(appId)).ReturnsAsync((TenantVerificationRequest?)null);
        
        TenantVerificationRequest? savedRequest = null;
        _requestRepoMock.Setup(x => x.AddAsync(It.IsAny<TenantVerificationRequest>()))
            .Callback<TenantVerificationRequest>(r => savedRequest = r)
            .Returns(Task.CompletedTask);

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedRequest);

        var result = await _service.InitiateVerificationAsync(appId, tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be("Draft");
        savedRequest.Should().NotBeNull();
        savedRequest.ApplicationId.Should().Be(appId);
        savedRequest.TenantId.Should().Be(tenantId);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldThrowBadRequestException_WhenDocumentTypeInvalid()
    {
        Func<Task> act = () => _service.UploadDocumentAsync(Guid.NewGuid(), new Mock<IFormFile>().Object, DocumentType.Aadhaar, Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldThrowNotFoundException_WhenRequestDoesNotExist()
    {
        var requestId = Guid.NewGuid();
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync((TenantVerificationRequest?)null);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("salary.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        Func<Task> act = () => _service.UploadDocumentAsync(requestId, fileMock.Object, DocumentType.SalarySlip, Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldThrowForbiddenException_WhenRequestBelongsToDifferentTenant()
    {
        var requestId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = Guid.NewGuid() };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("salary.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        Func<Task> act = () => _service.UploadDocumentAsync(requestId, fileMock.Object, DocumentType.SalarySlip, Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldThrowConflictException_WhenRequestIsNotDraft()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Pending };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("salary.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        Func<Task> act = () => _service.UploadDocumentAsync(requestId, fileMock.Object, DocumentType.SalarySlip, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldAddNewDocument_WhenDocumentTypeDoesNotExist()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Draft };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(x => x.FileName).Returns("salary.pdf");
        fileMock.Setup(x => x.ContentType).Returns("application/pdf");
        fileMock.Setup(x => x.Length).Returns(100);

        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/salary.pdf");

        var result = await _service.UploadDocumentAsync(requestId, fileMock.Object, DocumentType.SalarySlip, tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result.DocumentType.Should().Be("SalarySlip");
        result.DocumentUrl.Should().Be("/uploads/salary.pdf");
        request.TenantVerificationDocuments.Should().HaveCount(1);
        _documentRepoMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Once);
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldThrowBadRequestException_WhenFileIsNotPdf()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Draft };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(x => x.FileName).Returns("salary.png");
        fileMock.Setup(x => x.ContentType).Returns("image/png");
        fileMock.Setup(x => x.Length).Returns(100);

        Func<Task> act = () => _service.UploadDocumentAsync(requestId, fileMock.Object, DocumentType.SalarySlip, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only PDF documents are allowed.");
    }

    [Test]
    public async Task SubmitVerificationAsync_ShouldThrowBadRequestException_WhenRequiredDocumentsAreMissing()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Draft };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        Func<Task> act = () => _service.SubmitVerificationAsync(requestId, tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task SubmitVerificationAsync_ShouldTransitionToPending_WhenBothDocumentsExist()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Draft };
        request.TenantVerificationDocuments.Add(new TenantVerificationDocument
        {
            Document = new Document { DocumentType = DocumentType.SalarySlip, DocumentUrl = "url", OriginalFileName = "f", MimeType = "pdf" }
        });
        request.TenantVerificationDocuments.Add(new TenantVerificationDocument
        {
            Document = new Document { DocumentType = DocumentType.BankProof, DocumentUrl = "url", OriginalFileName = "f", MimeType = "pdf" }
        });

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        await _service.SubmitVerificationAsync(requestId, tenantId, CancellationToken.None);

        request.Status.Should().Be(ApprovalStatus.Pending);
        // _requestRepoMock.Verify(x => x.UpdateAsync(request), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReviewVerificationAsync_ShouldApproveRequest_WhenReviewerIsOwnerOfProperty()
    {
        var requestId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property { PropertyId = Guid.NewGuid(), OwnerId = ownerId, Title = "Prop", Description = "Test Property Description", AddressLine = "123 Test St" };
        var application = new RentalApplication { ApplicationId = Guid.NewGuid(), Property = property, Message = "msg" };
        var request = new TenantVerificationRequest { RequestId = requestId, RentalApplication = application, Status = ApprovalStatus.Pending };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var review = new ReviewTenantVerificationRequest { Approve = true, Remarks = "All looks good" };

        var result = await _service.ReviewVerificationAsync(requestId, review, ownerId, CancellationToken.None);

        result.Status.Should().Be("Approved");
        request.ReviewedById.Should().Be(ownerId);
        request.ReviewRemarks.Should().Be("All looks good");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReviewVerificationAsync_ShouldThrowForbiddenException_WhenReviewerIsNotOwner()
    {
        var requestId = Guid.NewGuid();
        var property = new Property { PropertyId = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Title = "Prop", Description = "Test Property Description", AddressLine = "123 Test St" };
        var application = new RentalApplication { ApplicationId = Guid.NewGuid(), Property = property, Message = "msg" };
        var request = new TenantVerificationRequest { RequestId = requestId, RentalApplication = application, Status = ApprovalStatus.Pending };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        var review = new ReviewTenantVerificationRequest { Approve = true, Remarks = "All looks good" };

        Func<Task> act = () => _service.ReviewVerificationAsync(requestId, review, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetPagedTenantRequestsAsync_ShouldReturnPagedList_WhenCalled()
    {
        var tenantId = Guid.NewGuid();
        var requests = new List<TenantVerificationRequest>
        {
            new TenantVerificationRequest { RequestId = Guid.NewGuid(), TenantId = tenantId, Status = ApprovalStatus.Draft }
        };
        _requestRepoMock.Setup(x => x.GetPagedByTenantIdAsync(tenantId, null, 1, 20))
            .ReturnsAsync((requests, 1));

        var result = await _service.GetPagedTenantRequestsAsync(tenantId, null, 1, 20, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Total.Should().Be(1);
        result.Data.Should().HaveCount(1);
        result.Data.First().Status.Should().Be("Draft");
    }

    [Test]
    public async Task GetPagedOwnerRequestsAsync_ShouldReturnPagedList_WhenCalled()
    {
        var ownerId = Guid.NewGuid();
        var requests = new List<TenantVerificationRequest>
        {
            new TenantVerificationRequest { RequestId = Guid.NewGuid(), Status = ApprovalStatus.Pending }
        };
        _requestRepoMock.Setup(x => x.GetPagedByOwnerIdAsync(ownerId, null, 1, 20))
            .ReturnsAsync((requests, 1));

        var result = await _service.GetPagedOwnerRequestsAsync(ownerId, null, 1, 20, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Total.Should().Be(1);
        result.Data.Should().HaveCount(1);
        result.Data.First().Status.Should().Be("Pending");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnRequest_WhenUserIsTenant()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = tenantId, Status = ApprovalStatus.Pending };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        // Act
        var result = await _service.GetByIdAsync(requestId, tenantId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be(requestId);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnRequest_WhenUserIsOwner()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var property = new Property { PropertyId = Guid.NewGuid(), OwnerId = ownerId, Title = "Prop", Description = "Desc", AddressLine = "123 St" };
        var application = new RentalApplication { ApplicationId = Guid.NewGuid(), Property = property, Message = "msg" };
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = Guid.NewGuid(), RentalApplication = application, Status = ApprovalStatus.Pending };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        // Act
        var result = await _service.GetByIdAsync(requestId, ownerId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be(requestId);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnRequest_WhenUserIsAdmin()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = Guid.NewGuid(), Status = ApprovalStatus.Pending };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        // Act
        var result = await _service.GetByIdAsync(requestId, adminId, true, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be(requestId);
    }

    [Test]
    public async Task GetByIdAsync_ShouldThrowForbiddenException_WhenUserIsNotAuthorized()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var property = new Property { PropertyId = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Title = "Prop", Description = "Desc", AddressLine = "123 St" };
        var application = new RentalApplication { ApplicationId = Guid.NewGuid(), Property = property, Message = "msg" };
        var request = new TenantVerificationRequest { RequestId = requestId, TenantId = Guid.NewGuid(), RentalApplication = application, Status = ApprovalStatus.Pending };
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(request);

        // Act & Assert
        Func<Task> act = async () => await _service.GetByIdAsync(requestId, randomUserId, false, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenRequestDoesNotExist()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync((TenantVerificationRequest?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.GetByIdAsync(requestId, Guid.NewGuid(), false, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
