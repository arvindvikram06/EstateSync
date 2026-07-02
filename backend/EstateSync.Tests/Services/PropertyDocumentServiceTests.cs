using System;
using System.Collections.Generic;
using System.IO;
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
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Properties;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PropertyDocumentServiceTests
{
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IDocumentRepository> _documentRepoMock;
    private Mock<IFileStorageService> _fileStorageServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private UserMapper _mapper;
    private Mock<ILogger<PropertyDocumentService>> _loggerMock;
    private PropertyDocumentService _propertyDocumentService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapper = new UserMapper();
        _loggerMock = new Mock<ILogger<PropertyDocumentService>>();

        _propertyDocumentService = new PropertyDocumentService(
            _propertyRepoMock.Object,
            _documentRepoMock.Object,
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldCreateNewDocument_WhenNotExists()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            Status = PropertyStatus.Draft,
            PropertyDocuments = new List<PropertyDocument>()
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("deed.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/properties/deed.pdf");

        // Act
        var result = await _propertyDocumentService.UploadDocumentAsync(propertyId, fileMock.Object, DocumentType.OwnershipCertificate, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be("OwnershipCertificate");
        result.DocumentUrl.Should().Be("/uploads/properties/deed.pdf");
        
        property.PropertyDocuments.Should().HaveCount(1);
        property.PropertyDocuments.First().Document.Should().NotBeNull();
        property.PropertyDocuments.First().Document!.DocumentType.Should().Be(DocumentType.OwnershipCertificate);

        _documentRepoMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Once);
        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadDocumentAsync_ShouldOverwriteExistingDocument_WhenAlreadyExists()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        
        var existingDoc = new Document
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.OwnershipCertificate,
            DocumentUrl = "/uploads/properties/old.pdf",
            OriginalFileName = "old.pdf",
            MimeType = "application/pdf",
            FileSize = 500,
            ReviewStatus = ApprovalStatus.Approved
        };

        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            Status = PropertyStatus.Draft,
            PropertyDocuments = new List<PropertyDocument>
            {
                new()
                {
                    PropertyId = propertyId,
                    DocumentId = existingDoc.DocumentId,
                    Document = existingDoc
                }
            }
        };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("new.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(2048);

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/properties/new.pdf");

        // Act
        var result = await _propertyDocumentService.UploadDocumentAsync(propertyId, fileMock.Object, DocumentType.OwnershipCertificate, ownerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OriginalFileName.Should().Be("new.pdf");
        result.DocumentUrl.Should().Be("/uploads/properties/new.pdf");
        result.ReviewStatus.Should().Be("Pending");

        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync("/uploads/properties/old.pdf"), Times.Once);
        _documentRepoMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Never);
        // _propertyRepoMock.Verify(x => x.UpdateAsync(property), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UploadDocumentAsync_ShouldThrowForbidden_WhenNotOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address"
        };

        var fileMock = new Mock<IFormFile>();

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        Func<Task> act = async () => await _propertyDocumentService.UploadDocumentAsync(propertyId, fileMock.Object, DocumentType.OwnershipCertificate, nonOwnerId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>();
    }


    [Test]
    public async Task GetPropertyDocumentsAsync_ShouldReturnDocuments_WhenUserIsOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var doc = new Document
        {
            DocumentId = documentId,
            DocumentType = DocumentType.OwnershipCertificate,
            DocumentUrl = "/uploads/properties/deed.pdf",
            OriginalFileName = "deed.pdf",
            MimeType = "application/pdf"
        };
        var link = new PropertyDocument
        {
            PropertyId = propertyId,
            DocumentId = documentId,
            Document = doc
        };
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            PropertyDocuments = new List<PropertyDocument> { link }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        var result = await _propertyDocumentService.GetPropertyDocumentsAsync(propertyId, ownerId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DocumentId.Should().Be(documentId);
    }

    [Test]
    public async Task GetPropertyDocumentsAsync_ShouldReturnDocuments_WhenUserIsAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var doc = new Document
        {
            DocumentId = documentId,
            DocumentType = DocumentType.OwnershipCertificate,
            DocumentUrl = "/uploads/properties/deed.pdf",
            OriginalFileName = "deed.pdf",
            MimeType = "application/pdf"
        };
        var link = new PropertyDocument
        {
            PropertyId = propertyId,
            DocumentId = documentId,
            Document = doc
        };
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            PropertyDocuments = new List<PropertyDocument> { link }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        var result = await _propertyDocumentService.GetPropertyDocumentsAsync(propertyId, adminId, true, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().DocumentId.Should().Be(documentId);
    }

    [Test]
    public void GetPropertyDocumentsAsync_ShouldThrowForbiddenException_WhenUserIsNeitherOwnerNorAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Test Property",
            Description = "Description",
            AddressLine = "Address",
            PropertyDocuments = new List<PropertyDocument>()
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        Func<Task> act = async () => await _propertyDocumentService.GetPropertyDocumentsAsync(propertyId, tenantId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public void GetPropertyDocumentsAsync_ShouldThrowNotFoundException_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync((Property?)null);

        // Act
        Func<Task> act = async () => await _propertyDocumentService.GetPropertyDocumentsAsync(propertyId, userId, false, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>();
    }
}
