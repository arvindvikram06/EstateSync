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
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Kyc;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Tests.Services;

[TestFixture]
public class KycServiceTests
{
    private Mock<IKycVerificationRepository> _kycRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IFileStorageService> _fileStorageMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<KycService>> _loggerMock;
    private UserMapper _mapper;

    private KycService _service;

    [SetUp]
    public void Setup()
    {
        _kycRepoMock = new Mock<IKycVerificationRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _fileStorageMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<KycService>>();
        _mapper = new UserMapper();

        _service = new KycService(
            _kycRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _fileStorageMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task CreateDraftAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        Func<Task> act = () => _service.CreateDraftAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User*");
    }

    [Test]
    public async Task CreateDraftAsync_ShouldThrowConflict_WhenKycAlreadyVerified()
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsKycVerified = true };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        Func<Task> act = () => _service.CreateDraftAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already verified*");
    }

    [Test]
    public async Task CreateDraftAsync_ShouldCreateNewDraft_WhenNoActiveExists()
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsKycVerified = false };
        var existingRecords = new List<KycVerification>();

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _kycRepoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(existingRecords);

        // We need GetWithDetailsByIdAsync to return the created record after it's saved.
        // For simplicity in testing, we use It.IsAny<Guid>()
        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new KycVerification { KycVerificationId = Guid.NewGuid(), UserId = userId, Status = ApprovalStatus.Draft });

        var result = await _service.CreateDraftAsync(userId, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(ApprovalStatus.Draft.ToString());
        
        _kycRepoMock.Verify(x => x.AddAsync(It.Is<KycVerification>(k => k.UserId == userId && k.Status == ApprovalStatus.Draft)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowNotFound_WhenKycDoesNotExist()
    {
        var kycId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(kycId)).ReturnsAsync((KycVerification?)null);

        Func<Task> act = () => _service.SubmitAsync(userId, kycId, new SubmitKycRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowForbidden_WhenUserDoesNotMatch()
    {
        var kycId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var kyc = new KycVerification { KycVerificationId = kycId, UserId = Guid.NewGuid() }; // Different user ID

        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(kycId)).ReturnsAsync(kyc);

        Func<Task> act = () => _service.SubmitAsync(userId, kycId, new SubmitKycRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowConflict_WhenNotDraft()
    {
        var kycId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var kyc = new KycVerification { KycVerificationId = kycId, UserId = userId, Status = ApprovalStatus.Pending };

        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(kycId)).ReturnsAsync(kyc);

        Func<Task> act = () => _service.SubmitAsync(userId, kycId, new SubmitKycRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*draft*");
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowBadRequest_WhenDocumentsMissing()
    {
        var kycId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var kyc = new KycVerification { KycVerificationId = kycId, UserId = userId, Status = ApprovalStatus.Draft };
        
        // No documents attached to KycVerification

        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(kycId)).ReturnsAsync(kyc);

        Func<Task> act = () => _service.SubmitAsync(userId, kycId, new SubmitKycRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Missing required documents*");
    }

    [Test]
    public async Task SubmitAsync_ShouldSucceed_WhenAllValidationsPass()
    {
        var kycId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new SubmitKycRequest { AadhaarNumber = "123412341234", PanNumber = "ABCDE1234F" };
        
        var kyc = new KycVerification 
        { 
            KycVerificationId = kycId, 
            UserId = userId, 
            Status = ApprovalStatus.Draft,
            KycDocuments = new List<KycDocument>
            {
                new KycDocument { Document = new Document { DocumentType = DocumentType.AadhaarFront } },
                new KycDocument { Document = new Document { DocumentType = DocumentType.PanCard } },
                new KycDocument { Document = new Document { DocumentType = DocumentType.AddressProof } }
            }
        };

        _kycRepoMock.Setup(x => x.GetWithDetailsByIdAsync(kycId)).ReturnsAsync(kyc);
        _kycRepoMock.Setup(x => x.IsAadhaarApprovedAsync(request.AadhaarNumber, userId)).ReturnsAsync(false);
        _kycRepoMock.Setup(x => x.IsPanApprovedAsync(request.PanNumber, userId)).ReturnsAsync(false);

        await _service.SubmitAsync(userId, kycId, request, CancellationToken.None);

        kyc.Status.Should().Be(ApprovalStatus.Pending);
        kyc.AadhaarNumber.Should().Be(request.AadhaarNumber);
        kyc.PanNumber.Should().Be(request.PanNumber);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
