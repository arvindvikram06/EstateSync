using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Users;

namespace EstateSync.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IRoleRepository> _roleRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private UserService _service;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new UserService(
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Test]
    public async Task BecomeOwnerAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync((User?)null);

        Func<Task> act = () => _service.BecomeOwnerAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task BecomeOwnerAsync_ShouldThrowForbiddenException_WhenUserNotKycVerified()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            FullName = "Test",
            Email = "t@t.com",
            PhoneNumber = "123",
            PasswordHash = "h",
            IsKycVerified = false
        };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);

        Func<Task> act = () => _service.BecomeOwnerAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*KYC*");
    }

    [Test]
    public async Task BecomeOwnerAsync_ShouldReturnAlreadyOwnerMessage_WhenUserAlreadyHasOwnerRole()
    {
        var userId = Guid.NewGuid();
        var ownerRole = new Role { RoleId = Guid.NewGuid(), Name = "Owner" };
        var user = new User
        {
            UserId = userId,
            FullName = "Test",
            Email = "t@t.com",
            PhoneNumber = "123",
            PasswordHash = "h",
            IsKycVerified = true,
            UserRoles = new List<UserRole>
            {
                new UserRole { UserId = userId, RoleId = ownerRole.RoleId, Role = ownerRole }
            }
        };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);

        var result = await _service.BecomeOwnerAsync(userId, CancellationToken.None);

        result.Should().Contain("already");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task BecomeOwnerAsync_ShouldAddOwnerRole_WhenKycVerifiedAndNotYetOwner()
    {
        var userId = Guid.NewGuid();
        var ownerRoleId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            FullName = "Test",
            Email = "t@t.com",
            PhoneNumber = "123",
            PasswordHash = "h",
            IsKycVerified = true,
            UserRoles = new List<UserRole>()
        };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);
        _roleRepoMock.Setup(x => x.GetByNameAsync("Owner"))
            .ReturnsAsync(new Role { RoleId = ownerRoleId, Name = "Owner" });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.BecomeOwnerAsync(userId, CancellationToken.None);

        result.Should().Contain("Owner role assigned");
        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == ownerRoleId);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
