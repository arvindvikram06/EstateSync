using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
    private Mock<ILogger<LeaseDocumentService>> _loggerMock;
    private LeaseDocumentService _service;

    [SetUp]
    public void Setup()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        
        _storageMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<LeaseDocumentService>>();

        _service = new LeaseDocumentService(_storageMock.Object, _loggerMock.Object);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenStorageIsNull()
    {
        Action act = () => new LeaseDocumentService(null!, _loggerMock.Object);
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
        var signaturePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var lease = new Lease
        {
            LeaseId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            MonthlyRent = 1500,
            SecurityDeposit = 3000,
            PaymentFrequency = LeasePaymentFrequency.Monthly,
            TenantSignatureImageUrl = "estatesync-private/leases/test/tenant-sig.png",
            OwnerSignatureImageUrl = "estatesync-private/leases/test/owner-sig.png",
            TenantSignedAt = DateTime.UtcNow,
            OwnerSignedAt = DateTime.UtcNow,
            Property = new Property { Title = "Test Prop" },
            Tenant = new User { FullName = "Tenant" }
        };

        _storageMock.Setup(x => x.SaveBytesAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "signed-agreement.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/leases/signed-agreement.pdf");
        _storageMock.Setup(x => x.ReadBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signaturePng);

        var url = await _service.GenerateSignedAgreementPdfAsync(lease, CancellationToken.None);

        url.Should().Be("/uploads/leases/signed-agreement.pdf");
        _storageMock.Verify(x => x.SaveBytesAsync(It.IsAny<byte[]>(), $"leases/{lease.LeaseId}", "signed-agreement.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }
}
