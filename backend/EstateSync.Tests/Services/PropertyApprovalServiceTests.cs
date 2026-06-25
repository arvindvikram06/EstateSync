using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PropertyApprovalServiceTests
{
    private Mock<IPropertyRepository> _propertyRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private PropertyMapper _propertyMapper;
    private Mock<ILogger<PropertyApprovalService>> _loggerMock;
    private PropertyApprovalService _propertyApprovalService;

    [SetUp]
    public void SetUp()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _propertyMapper = new PropertyMapper();
        _loggerMock = new Mock<ILogger<PropertyApprovalService>>();

        _propertyApprovalService = new PropertyApprovalService(
            _propertyRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _unitOfWorkMock.Object,
            _propertyMapper,
            _loggerMock.Object);
    }

    [Test]
    public async Task SubmitForApprovalAsync_ShouldChangeStatusToPendingApproval_AndNotifyAdmins()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Property Title",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            Status = PropertyStatus.Draft,
            PropertyDocuments = new List<PropertyDocument>
            {
                new()
                {
                    Document = new Document
                    {
                        DocumentType = DocumentType.OwnerShipProof,
                        DocumentUrl = "/docs/deed.pdf",
                        OriginalFileName = "deed.pdf",
                        MimeType = "application/pdf"
                    }
                }
            },
            PropertyImages = new List<PropertyImage>
            {
                new()
                {
                    ImageId = Guid.NewGuid(),
                    Image = "/images/prop.jpg",
                    IsPrimary = true
                }
            }
        };

        var admins = new List<User>
        {
            new User
            {
                UserId = Guid.NewGuid(),
                FullName = "Admin 1",
                Email = "admin1@example.com",
                PhoneNumber = "1234567890",
                PasswordHash = "hashed"
            }
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);
        _userRepoMock.Setup(x => x.GetUsersByRoleAsync("Admin")).ReturnsAsync(admins);

        // Act
        await _propertyApprovalService.SubmitForApprovalAsync(propertyId, ownerId, CancellationToken.None);

        // Assert
        property.Status.Should().Be(PropertyStatus.PendingApproval);
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == admins[0].UserId && n.Type == NotificationType.General)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SubmitForApprovalAsync_ShouldThrowBadRequestException_WhenNoImagesUploaded()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Property Title",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            Status = PropertyStatus.Draft,
            PropertyDocuments = new List<PropertyDocument>
            {
                new()
                {
                    Document = new Document
                    {
                        DocumentType = DocumentType.OwnerShipProof,
                        DocumentUrl = "/docs/deed.pdf",
                        OriginalFileName = "deed.pdf",
                        MimeType = "application/pdf"
                    }
                }
            },
            PropertyImages = new List<PropertyImage>()
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        Func<Task> act = async () => await _propertyApprovalService.SubmitForApprovalAsync(propertyId, ownerId, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<BadRequestException>().WithMessage("*at least one property image*");
    }

    [Test]
    public async Task ApproveOrRejectAsync_ShouldChangeStatusToActive_AndNotifyOwner_WhenApproved()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var property = new Property
        {
            PropertyId = propertyId,
            OwnerId = ownerId,
            Title = "Property Title",
            Description = "Test Description",
            AddressLine = "Test Address Line",
            Status = PropertyStatus.PendingApproval
        };

        _propertyRepoMock.Setup(x => x.GetWithDetailsByIdAsync(propertyId)).ReturnsAsync(property);

        // Act
        await _propertyApprovalService.ApproveOrRejectAsync(propertyId, adminId, "Active", "Approved remarks", CancellationToken.None);

        // Assert
        property.Status.Should().Be(PropertyStatus.Active);
        property.ApproveById.Should().Be(adminId);
        property.ApproveRemarks.Should().Be("Approved remarks");
        _notificationRepoMock.Verify(x => x.AddAsync(It.Is<Notification>(n => n.UserId == ownerId && n.Title == "Property Approved")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPendingApprovalPropertiesAsync_ShouldReturnPagedResponseOfPendingProperties()
    {
        // Arrange
        var properties = new List<Property>
        {
            new Property
            {
                PropertyId = Guid.NewGuid(),
                Title = "Pending Property 1",
                Description = "Description 1",
                AddressLine = "Address 1",
                Status = PropertyStatus.PendingApproval
            },
            new Property
            {
                PropertyId = Guid.NewGuid(),
                Title = "Pending Property 2",
                Description = "Description 2",
                AddressLine = "Address 2",
                Status = PropertyStatus.PendingApproval
            }
        };

        _propertyRepoMock.Setup(x => x.GetPendingApprovalPropertiesAsync(10, 0))
            .ReturnsAsync((properties, 2));

        // Act
        var result = await _propertyApprovalService.GetPendingApprovalPropertiesAsync(1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Page.Should().Be(1);
        result.Limit.Should().Be(10);
        result.Data!.First().Title.Should().Be("Pending Property 1");
    }
}
