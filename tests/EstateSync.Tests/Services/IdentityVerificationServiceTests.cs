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
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Services.IdentityVerification;
using EstateSync.Api.Interfaces.Services.IdentityVerification;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Tests.Services;

[TestFixture]
public class IdentityVerificationServiceTests
{
    private Mock<IIdentityVerificationRequestRepository> _requestRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private UserMapper _userMapper;
    private Mock<ILogger<IdentityVerificationService>> _loggerMock;
    private IdentityVerificationService _service;

    [SetUp]
    public void SetUp()
    {
        _requestRepoMock = new Mock<IIdentityVerificationRequestRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userMapper = new UserMapper();
        _loggerMock = new Mock<ILogger<IdentityVerificationService>>();

        _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<IdentityVerificationRequest>()))
            .Callback<IdentityVerificationRequest>(r => { if (r.RequestId == Guid.Empty) r.RequestId = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _documentRepoMock.Setup(d => d.AddAsync(It.IsAny<Document>()))
            .Callback<Document>(d => { if (d.DocumentId == Guid.Empty) d.DocumentId = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _service = new IdentityVerificationService(
            _requestRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _userMapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateDraftIdentityRequestAsync_ShouldCreateRequest_WhenNoActiveRequestsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, FullName = "Test", Email = "a@a.com", PhoneNumber = "123", PasswordHash = "h" });
        _requestRepoMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<IdentityVerificationRequest>());
        
        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new IdentityVerificationRequest
            {
                RequestId = id,
                UserId = userId,
                Status = ApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                IdentityVerificationDocuments = new List<IdentityVerificationDocument>()
            });

        // Act
        var result = await _service.CreateDraftIdentityRequestAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().NotBeEmpty();
        result.UserId.Should().Be(userId);
        result.Status.Should().Be("Draft");
        
        _requestRepoMock.Verify(r => r.AddAsync(It.Is<IdentityVerificationRequest>(q => q.UserId == userId && q.Status == ApprovalStatus.Draft)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateDraftIdentityRequestAsync_ShouldThrowConflictException_WhenPendingOrApprovedRequestExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, FullName = "Test", Email = "a@a.com", PhoneNumber = "123", PasswordHash = "h" });
        var requests = new List<IdentityVerificationRequest>
        {
            new IdentityVerificationRequest { Status = ApprovalStatus.Pending }
        };
        _requestRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(requests);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateDraftIdentityRequestAsync(userId, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Test]
    public async Task UploadIdentityDocumentAsync_ShouldUploadFileAndLinkDocument_WhenRequestIsDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("aadhaar.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(500);

        var requestEntity = new IdentityVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            IdentityVerificationDocuments = new List<IdentityVerificationDocument>()
        };

        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _fileStorageServiceMock.Setup(s => s.SaveFileAsync(fileMock.Object, "identity-verification", It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://storage/aadhaar.pdf");

        // Act
        var result = await _service.UploadIdentityDocumentAsync(userId, requestId, fileMock.Object, DocumentType.Aadhaar, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("Aadhaar");
        result.DocumentUrl.Should().Be("http://storage/aadhaar.pdf");
        _documentRepoMock.Verify(d => d.AddAsync(It.IsAny<Document>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadIdentityDocumentAsync_ShouldThrowBadRequestException_WhenFileIsNotPdf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("aadhaar.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");
        fileMock.Setup(f => f.Length).Returns(500);

        var requestEntity = new IdentityVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            IdentityVerificationDocuments = new List<IdentityVerificationDocument>()
        };

        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act
        Func<Task> act = async () => await _service.UploadIdentityDocumentAsync(userId, requestId, fileMock.Object, DocumentType.Aadhaar, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only PDF documents are allowed.");
    }

    [Test]
    public async Task SubmitIdentityRequestAsync_ShouldSubmitRequest_WhenBothDocsPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var request = new SubmitIdentityVerificationRequest
        {
            AadhaarNumber = "123456789012",
            PanNumber = "ABCDE1234F"
        };

        var requestEntity = new IdentityVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            IdentityVerificationDocuments = new List<IdentityVerificationDocument>
            {
                new IdentityVerificationDocument 
                { 
                    Document = new Document 
                    { 
                        DocumentType = DocumentType.Aadhaar,
                        DocumentUrl = "http://storage/aadhaar.png",
                        OriginalFileName = "aadhaar.png",
                        MimeType = "image/png"
                    } 
                },
                new IdentityVerificationDocument 
                { 
                    Document = new Document 
                    { 
                        DocumentType = DocumentType.PAN,
                        DocumentUrl = "http://storage/pan.png",
                        OriginalFileName = "pan.png",
                        MimeType = "image/png"
                    } 
                }
            }
        };

        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act
        await _service.SubmitIdentityRequestAsync(userId, requestId, request, CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Pending);
        requestEntity.AadhaarNumber.Should().Be("123456789012");
        requestEntity.PanNumber.Should().Be("ABCDE1234F");
        // _requestRepoMock.Verify(r => r.UpdateAsync(requestEntity), Times.Once);
    }

    [Test]
    public async Task SubmitIdentityRequestAsync_ShouldThrowBadRequestException_WhenDocumentsAreMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var request = new SubmitIdentityVerificationRequest
        {
            AadhaarNumber = "123456789012",
            PanNumber = "ABCDE1234F"
        };

        var requestEntity = new IdentityVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            IdentityVerificationDocuments = new List<IdentityVerificationDocument>() // No documents uploaded
        };

        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act & Assert
        Func<Task> act = async () => await _service.SubmitIdentityRequestAsync(userId, requestId, request, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Missing required document types: Aadhaar, PAN");
    }

    [Test]
    public async Task VerifyIdentityRequestAsync_ShouldUpdateUserAndApproveDocuments_WhenApproved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var document = new Document 
        { 
            DocumentType = DocumentType.Aadhaar, 
            ReviewStatus = ApprovalStatus.Pending,
            DocumentUrl = "http://storage/aadhaar.png",
            OriginalFileName = "aadhaar.png",
            MimeType = "image/png"
        };
        
        var requestEntity = new IdentityVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Pending,
            AadhaarNumber = "123456789012",
            PanNumber = "ABCDE1234F",
            IdentityVerificationDocuments = new List<IdentityVerificationDocument>
            {
                new IdentityVerificationDocument { Document = document }
            }
        };

        var userEntity = new User
        {
            UserId = userId,
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "12345",
            PasswordHash = "hash"
        };

        _requestRepoMock.Setup(r => r.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _userRepoMock.Setup(u => u.GetByIdWithRolesAsync(userId)).ReturnsAsync(userEntity);

        // Act
        await _service.VerifyIdentityRequestAsync(requestId, adminId, "Approved", "All good", CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Approved);
        userEntity.IsIdentityVerified.Should().BeTrue();
        userEntity.AadhaarNumber.Should().Be("123456789012");
        userEntity.PanNumber.Should().Be("ABCDE1234F");
        document.ReviewStatus.Should().Be(ApprovalStatus.Approved);

        // _userRepoMock.Verify(u => u.UpdateAsync(userEntity), Times.Once);
        // _documentRepoMock.Verify(d => d.UpdateAsync(document), Times.Once);
        _notificationRepoMock.Verify(n => n.AddAsync(It.IsAny<Notification>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
