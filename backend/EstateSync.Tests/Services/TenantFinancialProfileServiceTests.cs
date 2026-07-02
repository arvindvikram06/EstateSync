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
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.TenantFinancialProfile;

namespace EstateSync.Tests.Services;

[TestFixture]
public class TenantFinancialProfileServiceTests
{
    private Mock<ITenantFinancialProfileRepository> _profileRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IFileStorageService> _storageMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<TenantFinancialProfileService>> _loggerMock;
    private UserMapper _mapper;
    private TenantFinancialProfileService _service;

    [SetUp]
    public void Setup()
    {
        _profileRepoMock = new Mock<ITenantFinancialProfileRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _storageMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TenantFinancialProfileService>>();
        _mapper = new UserMapper();

        _service = new TenantFinancialProfileService(
            _profileRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _storageMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    // --- CreateDraftAsync ---

    [Test]
    public async Task CreateDraftAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        var tenantId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync((User?)null);
        Func<Task> act = () => _service.CreateDraftAsync(tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateDraftAsync_ShouldThrowForbidden_WhenNotKycVerified()
    {
        var tenantId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(new User { IsKycVerified = false });
        Func<Task> act = () => _service.CreateDraftAsync(tenantId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*KYC verification*");
    }

    [Test]
    public async Task CreateDraftAsync_ShouldCreateNewDraft_WhenValid()
    {
        var tenantId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(new User { IsKycVerified = true });
        _profileRepoMock.Setup(x => x.GetByTenantIdAsync(tenantId)).ReturnsAsync(new List<TenantFinancialProfile>());

        _profileRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new TenantFinancialProfile { Status = ApprovalStatus.Draft });

        var result = await _service.CreateDraftAsync(tenantId, CancellationToken.None);
        result.Should().NotBeNull();
        _profileRepoMock.Verify(x => x.AddAsync(It.IsAny<TenantFinancialProfile>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- SubmitAsync ---

    [Test]
    public async Task SubmitAsync_ShouldThrowNotFound_WhenProfileDoesNotExist()
    {
        _profileRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TenantFinancialProfile?)null);
        Func<Task> act = () => _service.SubmitAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitTenantFinancialProfileRequest(), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowForbidden_WhenUserDoesNotMatch()
    {
        var profile = new TenantFinancialProfile { TenantId = Guid.NewGuid() };
        _profileRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);
        Func<Task> act = () => _service.SubmitAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitTenantFinancialProfileRequest(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task SubmitAsync_ShouldThrowBadRequest_WhenDocumentsMissing()
    {
        var tenantId = Guid.NewGuid();
        var profile = new TenantFinancialProfile { TenantId = tenantId, Status = ApprovalStatus.Draft };
        _profileRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        var request = new SubmitTenantFinancialProfileRequest { EmploymentType = EmploymentType.Salaried };
        Func<Task> act = () => _service.SubmitAsync(tenantId, Guid.NewGuid(), request, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Missing required documents*");
    }

    [Test]
    public async Task SubmitAsync_ShouldSucceed_WhenDocumentsPresent()
    {
        var tenantId = Guid.NewGuid();
        var profile = new TenantFinancialProfile 
        { 
            TenantId = tenantId, 
            Status = ApprovalStatus.Draft,
            TenantFinancialDocuments = new List<TenantFinancialDocument>
            {
                new TenantFinancialDocument { Document = new Document { DocumentType = DocumentType.SalarySlip } },
                new TenantFinancialDocument { Document = new Document { DocumentType = DocumentType.EmploymentLetter } },
                new TenantFinancialDocument { Document = new Document { DocumentType = DocumentType.BankStatement } }
            }
        };

        _profileRepoMock.Setup(x => x.GetWithDetailsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        var request = new SubmitTenantFinancialProfileRequest { EmploymentType = EmploymentType.Salaried };
        await _service.SubmitAsync(tenantId, Guid.NewGuid(), request, CancellationToken.None);

        profile.Status.Should().Be(ApprovalStatus.Pending);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
