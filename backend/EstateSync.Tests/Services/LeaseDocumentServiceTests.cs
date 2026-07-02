using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Leases;

namespace EstateSync.Tests.Services;

[TestFixture]
public class LeaseDocumentServiceTests
{
    private Mock<IFileStorageService> _storageMock;
    private Mock<IWebHostEnvironment> _envMock;
    private Mock<ILogger<LeaseDocumentService>> _loggerMock;
    private LeaseDocumentService _service;
    private string _tempPath;

    [SetUp]
    public void Setup()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        
        _storageMock = new Mock<IFileStorageService>();
        _envMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<LeaseDocumentService>>();

        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
        _envMock.Setup(x => x.ContentRootPath).Returns(_tempPath);

        _service = new LeaseDocumentService(_storageMock.Object, _envMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenStorageIsNull()
    {
        Action act = () => new LeaseDocumentService(null!, _envMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GenerateAgreementPdfAsync_ShouldGenerateAndSavePdf()
    {
        var lease = new Lease
        {
            LeaseId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            MonthlyRent = 1500,
            SecurityDeposit = 3000,
            PaymentFrequency = LeasePaymentFrequency.Monthly,
            Property = new Property { Title = "Test Prop", AddressLine = "123 Main St", Owner = new User { FullName = "Owner" } },
            Tenant = new User { FullName = "Tenant" }
        };

        _storageMock.Setup(x => x.SaveBytesAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "agreement.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/agreement.pdf");

        var url = await _service.GenerateAgreementPdfAsync(lease, CancellationToken.None);

        url.Should().Be("/uploads/leases/agreement.pdf");
        _storageMock.Verify(x => x.SaveBytesAsync(It.IsAny<byte[]>(), $"leases/{lease.LeaseId}", "agreement.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GenerateSignedAgreementPdfAsync_ShouldGenerateAndSavePdf_WithSignatures()
    {
        // Mock signatures to exist on disk
        var tenantSigPath = Path.Combine(_tempPath, "tenant-sig.png");
        var ownerSigPath = Path.Combine(_tempPath, "owner-sig.png");
        await File.WriteAllBytesAsync(tenantSigPath, new byte[] { 1, 2, 3 });
        await File.WriteAllBytesAsync(ownerSigPath, new byte[] { 1, 2, 3 });

        var lease = new Lease
        {
            LeaseId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            MonthlyRent = 1500,
            SecurityDeposit = 3000,
            PaymentFrequency = LeasePaymentFrequency.Monthly,
            TenantSignatureImageUrl = "/tenant-sig.png",
            OwnerSignatureImageUrl = "/owner-sig.png",
            TenantSignedAt = DateTime.UtcNow,
            OwnerSignedAt = DateTime.UtcNow,
            Property = new Property { Title = "Test Prop" },
            Tenant = new User { FullName = "Tenant" }
        };

        _storageMock.Setup(x => x.SaveBytesAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "signed-agreement.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/signed-agreement.pdf");

        var url = await _service.GenerateSignedAgreementPdfAsync(lease, CancellationToken.None);

        url.Should().Be("/uploads/leases/signed-agreement.pdf");
        _storageMock.Verify(x => x.SaveBytesAsync(It.IsAny<byte[]>(), $"leases/{lease.LeaseId}", "signed-agreement.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }
}
