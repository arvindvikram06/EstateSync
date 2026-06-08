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
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Storage;
using EstateSync.Api.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Tests.Services;

[TestFixture]
public class OwnerVerificationServiceTests
{
    private Mock<IOwnerVerificationRequestRepository> _requestRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private UserMapper _userMapper;
    private Mock<ILogger<OwnerVerificationService>> _loggerMock;
    private OwnerVerificationService _verificationService;

    [SetUp]
    public void SetUp()
    {
        _requestRepoMock = new Mock<IOwnerVerificationRequestRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userMapper = new UserMapper();
        _loggerMock = new Mock<ILogger<OwnerVerificationService>>();

        _verificationService = new OwnerVerificationService(
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
    public void SubmitVerificationRequestAsync_ShouldThrowBadRequestException_WhenMissingRequiredTypes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var aadhaarId = Guid.NewGuid();

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>
            {
                new OwnerVerificationDocument
                {
                    OwnerVerificationDocumentId = Guid.NewGuid(),
                    RequestId = requestId,
                    DocumentId = aadhaarId,
                    Document = new Document
                    {
                        DocumentId = aadhaarId,
                        DocumentType = DocumentType.Aadhaar,
                        DocumentUrl = "url",
                        OriginalFileName = "aadhaar.pdf",
                        MimeType = "application/pdf"
                    }
                }
            }
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act
        Func<Task> act = async () => await _verificationService.SubmitVerificationRequestAsync(userId, requestId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Missing required document types*");
    }

    [Test]
    public async Task SubmitVerificationRequestAsync_ShouldSubmitRequest_WhenAllDocumentsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var aadhaarId = Guid.NewGuid();
        var panId = Guid.NewGuid();
        var bankProofId = Guid.NewGuid();

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>
            {
                new OwnerVerificationDocument { DocumentId = aadhaarId, Document = new Document { DocumentId = aadhaarId, DocumentType = DocumentType.Aadhaar, DocumentUrl = "url", OriginalFileName = "a.pdf", MimeType = "application/pdf" } },
                new OwnerVerificationDocument { DocumentId = panId, Document = new Document { DocumentId = panId, DocumentType = DocumentType.PAN, DocumentUrl = "url", OriginalFileName = "p.pdf", MimeType = "application/pdf" } },
                new OwnerVerificationDocument { DocumentId = bankProofId, Document = new Document { DocumentId = bankProofId, DocumentType = DocumentType.BankProof, DocumentUrl = "url", OriginalFileName = "b.pdf", MimeType = "application/pdf" } }
            }
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act
        await _verificationService.SubmitVerificationRequestAsync(userId, requestId, CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Pending);
        _requestRepoMock.Verify(x => x.UpdateAsync(requestEntity), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateDraftVerificationRequestAsync_ShouldCreateNewDraft_WhenNoActiveOrDraftRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _requestRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(new List<OwnerVerificationRequest>());

        // Act
        var result = await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _requestRepoMock.Verify(x => x.AddAsync(It.Is<OwnerVerificationRequest>(r => r.UserId == userId && r.Status == ApprovalStatus.Draft)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateDraftVerificationRequestAsync_ShouldReturnExistingDraft_WhenDraftAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingDraftId = Guid.NewGuid();
        var existingRequests = new List<OwnerVerificationRequest>
        {
            new OwnerVerificationRequest { RequestId = existingDraftId, UserId = userId, Status = ApprovalStatus.Draft }
        };
        _requestRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(existingRequests);

        // Act
        var result = await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(existingDraftId);
        _requestRepoMock.Verify(x => x.AddAsync(It.IsAny<OwnerVerificationRequest>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateDraftVerificationRequestAsync_ShouldThrowConflictException_WhenPendingOrApprovedRequestExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingRequests = new List<OwnerVerificationRequest>
        {
            new OwnerVerificationRequest { RequestId = Guid.NewGuid(), UserId = userId, Status = ApprovalStatus.Pending }
        };
        _requestRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(existingRequests);

        // Act
        Func<Task> act = async () => await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ConflictException>().WithMessage("*active or approved*");
    }

    [Test]
    public async Task UploadVerificationDocumentAsync_ShouldLinkNewDocument_WhenNoDocumentOfThatTypeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("pan.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(150);

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/pan.pdf");

        // Act
        var result = await _verificationService.UploadVerificationDocumentAsync(userId, requestId, fileMock.Object, DocumentType.PAN, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("PAN");
        result.DocumentUrl.Should().Be("/uploads/pan.pdf");
        
        requestEntity.OwnerVerificationDocuments.Should().ContainSingle(d => d.Document!.DocumentType == DocumentType.PAN);
        _documentRepoMock.Verify(x => x.AddAsync(It.Is<Document>(d => d.DocumentType == DocumentType.PAN)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadVerificationDocumentAsync_ShouldUpdateDocumentAndCleanOldFile_WhenDocumentOfThatTypeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var existingDocId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("new_pan.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(200);

        var existingDoc = new Document
        {
            DocumentId = existingDocId,
            DocumentType = DocumentType.PAN,
            DocumentUrl = "/uploads/old_pan.pdf",
            OriginalFileName = "old_pan.pdf",
            MimeType = "application/pdf"
        };

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>
            {
                new OwnerVerificationDocument
                {
                    RequestId = requestId,
                    DocumentId = existingDocId,
                    Document = existingDoc
                }
            }
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/new_pan.pdf");

        // Act
        var result = await _verificationService.UploadVerificationDocumentAsync(userId, requestId, fileMock.Object, DocumentType.PAN, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().Be(existingDocId);
        result.DocumentUrl.Should().Be("/uploads/new_pan.pdf");

        existingDoc.DocumentUrl.Should().Be("/uploads/new_pan.pdf");
        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync("/uploads/old_pan.pdf"), Times.Once);
        _documentRepoMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task VerifyRequestAsync_ShouldApproveRequest_UpdateUserRoles_AndNotifyUser()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Pending,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
        };

        var user = new User
        {
            UserId = userId,
            FullName = "User Name",
            Email = "user@example.com",
            PhoneNumber = "1234567890",
            PasswordHash = "hashed",
            UserRoles = new List<UserRole>()
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _userRepoMock.Setup(x => x.GetWithRolesByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _verificationService.VerifyRequestAsync(requestId, adminId, "Approved", "All looks good.", CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Approved);
        requestEntity.ReviewedById.Should().Be(adminId);
        requestEntity.ReviewRemarks.Should().Be("All looks good.");

        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == Guid.Parse("22222222-2222-2222-2222-222222222222"));

        _requestRepoMock.Verify(x => x.UpdateAsync(requestEntity), Times.Once);
        _userRepoMock.Verify(x => x.UpdateAsync(user), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == userId && n.Type == NotificationType.VerificationApproved)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
