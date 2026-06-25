using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Admin;

namespace EstateSync.Tests.Services;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IRoleRepository> _roleRepoMock;
    private Mock<IPasswordService> _passwordServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILeaseService> _leaseServiceMock;
    private AdminService _service;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _leaseServiceMock = new Mock<ILeaseService>();

        _service = new AdminService(
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _passwordServiceMock.Object,
            _unitOfWorkMock.Object,
            _leaseServiceMock.Object);
    }

    // ─── CreateVerifierAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateVerifierAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        var request = new CreateStaffRequest("Jane", "jane@test.com", "9999999999", "Pass123!");
        _userRepoMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User { UserId = Guid.NewGuid(), Email = request.Email, FullName = "Jane", PhoneNumber = "9999999999", PasswordHash = "h" });

        Func<Task> act = () => _service.CreateVerifierAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email already exists*");
    }

    [Test]
    public async Task CreateVerifierAsync_ShouldCreateVerifierWithRole_WhenEmailIsNew()
    {
        var request = new CreateStaffRequest("Jane", "jane@test.com", "9999999999", "Pass123!");
        var verifierRoleId = Guid.NewGuid();

        _userRepoMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepoMock.Setup(x => x.GetByNameAsync("Verifier"))
            .ReturnsAsync(new Role { RoleId = verifierRoleId, Name = "Verifier" });
        _passwordServiceMock.Setup(x => x.HashPassword(request.Password)).Returns("hashed");
        _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await _service.CreateVerifierAsync(request, CancellationToken.None);

        _userRepoMock.Verify(x => x.AddAsync(It.Is<User>(u =>
            u.Email == request.Email &&
            u.FullName == request.FullName &&
            u.IsActive == true &&
            u.IsKycVerified == true &&
            u.UserRoles.Count == 1 &&
            u.UserRoles.Any(ur => ur.RoleId == verifierRoleId))), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── DeactivateStaffAsync ───────────────────────────────────────────────

    [Test]
    public async Task DeactivateStaffAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync((User?)null);

        Func<Task> act = () => _service.DeactivateStaffAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task DeactivateStaffAsync_ShouldThrowBadRequestException_WhenUserIsNotVerifier()
    {
        var userId = Guid.NewGuid();
        var tenantRole = new Role { RoleId = Guid.NewGuid(), Name = "Tenant" };
        var user = new User
        {
            UserId = userId,
            FullName = "Bob",
            Email = "bob@test.com",
            PhoneNumber = "123",
            PasswordHash = "h",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole { UserId = userId, RoleId = tenantRole.RoleId, Role = tenantRole }
            }
        };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);

        Func<Task> act = () => _service.DeactivateStaffAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*not a verifier*");
    }

    [Test]
    public async Task DeactivateStaffAsync_ShouldSetIsActiveFalse_WhenUserIsVerifier()
    {
        var userId = Guid.NewGuid();
        var verifierRole = new Role { RoleId = Guid.NewGuid(), Name = "Verifier" };
        var user = new User
        {
            UserId = userId,
            FullName = "Bob",
            Email = "bob@test.com",
            PhoneNumber = "123",
            PasswordHash = "h",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole { UserId = userId, RoleId = verifierRole.RoleId, Role = verifierRole }
            }
        };
        _userRepoMock.Setup(x => x.GetByIdWithRolesAsync(userId)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await _service.DeactivateStaffAsync(userId, CancellationToken.None);

        user.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
