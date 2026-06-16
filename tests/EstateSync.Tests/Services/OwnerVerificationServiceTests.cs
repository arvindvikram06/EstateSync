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
using EstateSync.Api.DTOs.OwnerVerification;
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
    private Mock<IRoleRepository> _roleRepoMock;
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
        _roleRepoMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userMapper = new UserMapper();
        _loggerMock = new Mock<ILogger<OwnerVerificationService>>();

        _verificationService = new OwnerVerificationService(
            _requestRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _fileStorageServiceMock.Object,
            _roleRepoMock.Object,
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

        var requestDto = new SubmitOwnerVerificationRequest
        {
            PanOrTaxId = "ABCDE1234F"
        };

        // Act
        Func<Task> act = async () => await _verificationService.SubmitVerificationRequestAsync(userId, requestId, requestDto, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Missing required documents*");
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
        var addressProofId = Guid.NewGuid();

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>
            {
                new OwnerVerificationDocument { DocumentId = aadhaarId, Document = new Document { DocumentId = aadhaarId, DocumentType = DocumentType.Aadhaar, DocumentUrl = "url", OriginalFileName = "a.pdf", MimeType = "application/pdf" } },
                new OwnerVerificationDocument { DocumentId = panId, Document = new Document { DocumentId = panId, DocumentType = DocumentType.PAN, DocumentUrl = "url", OriginalFileName = "p.pdf", MimeType = "application/pdf" } },
                new OwnerVerificationDocument { DocumentId = bankProofId, Document = new Document { DocumentId = bankProofId, DocumentType = DocumentType.BankProof, DocumentUrl = "url", OriginalFileName = "b.pdf", MimeType = "application/pdf" } },
                new OwnerVerificationDocument { DocumentId = addressProofId, Document = new Document { DocumentId = addressProofId, DocumentType = DocumentType.AddressProof, DocumentUrl = "url", OriginalFileName = "addr.pdf", MimeType = "application/pdf" } }
            }
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        var requestDto = new SubmitOwnerVerificationRequest
        {
            PanOrTaxId = "ABCDE1234F"
        };

        // Act
        await _verificationService.SubmitVerificationRequestAsync(userId, requestId, requestDto, CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Pending);
        // _requestRepoMock.Verify(x => x.UpdateAsync(requestEntity), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateDraftVerificationRequestAsync_ShouldCreateNewDraft_WhenNoActiveOrDraftRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsIdentityVerified = true, FullName = "Owner", Email = "o@o.com", PasswordHash = "hash", PhoneNumber = "123456789" };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _requestRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(new List<OwnerVerificationRequest>());
        
        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new OwnerVerificationRequest
            {
                RequestId = id,
                UserId = userId,
                User = user,
                Status = ApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
            });

        // Act
        var result = await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().NotBeEmpty();
        result.UserId.Should().Be(userId);
        result.Status.Should().Be("Draft");
        
        _requestRepoMock.Verify(x => x.AddAsync(It.Is<OwnerVerificationRequest>(r => r.UserId == userId && r.Status == ApprovalStatus.Draft)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateDraftVerificationRequestAsync_ShouldReturnExistingDraft_WhenDraftAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsIdentityVerified = true, FullName = "Owner", Email = "o@o.com", PasswordHash = "hash", PhoneNumber = "123456789" };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        var existingDraftId = Guid.NewGuid();
        var existingRequests = new List<OwnerVerificationRequest>
        {
            new OwnerVerificationRequest { RequestId = existingDraftId, UserId = userId, Status = ApprovalStatus.Draft }
        };
        _requestRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(existingRequests);

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(existingDraftId))
            .ReturnsAsync(new OwnerVerificationRequest
            {
                RequestId = existingDraftId,
                UserId = userId,
                User = user,
                Status = ApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
            });

        // Act
        var result = await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequestId.Should().Be(existingDraftId);
        result.UserId.Should().Be(userId);
        
        _requestRepoMock.Verify(x => x.AddAsync(It.IsAny<OwnerVerificationRequest>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateDraftVerificationRequestAsync_ShouldThrowConflictException_WhenPendingOrApprovedRequestExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsIdentityVerified = true, FullName = "Owner", Email = "o@o.com", PasswordHash = "hash", PhoneNumber = "123456789" };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
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
    public void CreateDraftVerificationRequestAsync_ShouldThrowForbiddenException_WhenUserNotIdentityVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsIdentityVerified = false, FullName = "Owner", Email = "o@o.com", PasswordHash = "hash", PhoneNumber = "123456789" };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public void CreateDraftVerificationRequestAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId))
            .ThrowsAsync(new NotFoundException("User", userId));

        // Act
        Func<Task> act = async () => await _verificationService.CreateDraftVerificationRequestAsync(userId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>();
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
    public async Task UploadVerificationDocumentAsync_ShouldDeleteOldDocumentAndLinkNewDocument_WhenDocumentOfThatTypeExists()
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
        result.DocumentUrl.Should().Be("/uploads/new_pan.pdf");

        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync("/uploads/old_pan.pdf"), Times.Once);
        // _documentRepoMock.Verify(x => x.UpdateAsync(existingDoc), Times.Once);
        _documentRepoMock.Verify(x => x.DeleteAsync(It.IsAny<Document>()), Times.Never);
        _documentRepoMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadVerificationDocumentAsync_ShouldThrowBadRequestException_WhenFileIsNotPdf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("pan.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");
        fileMock.Setup(f => f.Length).Returns(150);

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            Status = ApprovalStatus.Draft,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);

        // Act
        Func<Task> act = async () => await _verificationService.UploadVerificationDocumentAsync(userId, requestId, fileMock.Object, DocumentType.PAN, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only PDF documents are allowed.");
    }

    [Test]
    public async Task VerifyRequestAsync_ShouldApproveRequest_UpdateUserRoles_AndNotifyUser()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ownerRoleId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            FullName = "User Name",
            Email = "user@example.com",
            PhoneNumber = "1234567890",
            PasswordHash = "hashed",
            UserRoles = new List<UserRole>()
        };

        var requestEntity = new OwnerVerificationRequest
        {
            RequestId = requestId,
            UserId = userId,
            User = user,
            Status = ApprovalStatus.Pending,
            OwnerVerificationDocuments = new List<OwnerVerificationDocument>()
        };

        _requestRepoMock.Setup(x => x.GetWithDetailsByIdAsync(requestId)).ReturnsAsync(requestEntity);
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);
        _roleRepoMock.Setup(x => x.GetByNameAsync("Owner")).ReturnsAsync(new Role
        {
            RoleId = ownerRoleId,
            Name = "Owner"
        });

        // Act
        await _verificationService.VerifyRequestAsync(requestId, adminId, "Approved", "All looks good.", CancellationToken.None);

        // Assert
        requestEntity.Status.Should().Be(ApprovalStatus.Approved);
        requestEntity.ReviewedById.Should().Be(adminId);
        requestEntity.ReviewRemarks.Should().Be("All looks good.");

        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == ownerRoleId);

        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == userId && n.Type == NotificationType.VerificationApproved)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
