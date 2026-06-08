using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.Auth;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Auth;
using EstateSync.Api.Services.Tokens;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Tokens;

namespace EstateSync.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IRepository<Role>> _roleRepositoryMock;
    private Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private Mock<IPasswordService> _passwordServiceMock;
    private Mock<ITokenService> _tokenServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private AuthService _authService;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _passwordServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            new EstateSync.Api.Mappers.AuthMapper()
        );
    }

    [Test]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "New User",
            Email = "new@example.com",
            PhoneNumber = "1234567890",
            Password = "Password123!"
        };
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        var tenantRole = new Role { RoleId = Guid.NewGuid(), Name = "Tenant" };
        _roleRepositoryMock.Setup(x => x.GetAllAsync(1, 100))
            .ReturnsAsync((new List<Role> { tenantRole }, 1));

        _passwordServiceMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_pwd");

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var (response, refreshToken) = await _authService.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("access_token");
        response.Email.Should().Be(request.Email);
        response.Roles.Should().Contain("Tenant");
        refreshToken.Should().Be("refresh_token");

        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == request.Email && u.FullName == request.FullName)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public void RegisterAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Existing User",
            Email = "existing@example.com",
            PhoneNumber = "1234567890",
            Password = "Password123!"
        };
        var existingUser = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = "hash",
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request);

        // Assert
        act.Should().ThrowAsync<ConflictException>().WithMessage("Email is already registered.");
    }

    [Test]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            FullName = "John Doe",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            IsActive = true
        };
        
        _userRepositoryMock.Setup(x => x.GetWithRolesByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var (response, refreshToken) = await _authService.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("access_token");
        response.UserId.Should().Be(user.UserId);
        refreshToken.Should().Be("refresh_token");

        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt => rt.UserId == user.UserId && rt.Token == "refresh_token")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public void LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong_pwd"
        };
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            FullName = "John Doe",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetWithRolesByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request);

        // Assert
        act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Test]
    public void LoginAsync_ShouldThrowForbiddenException_WhenUserIsBanned()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            FullName = "John Doe",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            IsActive = false
        };

        _userRepositoryMock.Setup(x => x.GetWithRolesByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request);

        // Assert
        act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenTokenIsValid()
    {
        // Arrange
        var oldToken = "valid_old_token";
        var tokenEntity = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = oldToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            TokenFamily = "family_id",
            RevokedAt = null
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(oldToken))
            .ReturnsAsync(tokenEntity);

        var user = new User
        {
            UserId = tokenEntity.UserId,
            Email = "user@example.com",
            FullName = "John Doe",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            IsActive = true
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(tokenEntity.UserId))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.GetWithRolesByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("new_access_token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        // Act
        var (response, refreshToken) = await _authService.RefreshTokenAsync(oldToken);

        // Assert
        response.AccessToken.Should().Be("new_access_token");
        refreshToken.Should().Be("new_refresh_token");
        tokenEntity.RevokedAt.Should().NotBeNull();

        _refreshTokenRepositoryMock.Verify(x => x.UpdateAsync(tokenEntity), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt => rt.TokenFamily == "family_id")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldRevokeFamily_WhenTokenIsAlreadyRevoked()
    {
        // Arrange
        var oldToken = "already_revoked_token";
        var tokenEntity = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = oldToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            TokenFamily = "family_id",
            RevokedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(oldToken))
            .ReturnsAsync(tokenEntity);

        var activeTokens = new List<RefreshToken>
        {
            new() { Token = "another_token_in_family", TokenFamily = "family_id", RevokedAt = null }
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetActiveTokensInFamilyAsync(tokenEntity.TokenFamily))
            .ReturnsAsync(activeTokens);

        // Act
        Func<Task> act = async () => await _authService.RefreshTokenAsync(oldToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Token family reuse detected. Please log in again.");
        activeTokens.ForEach(t => t.RevokedAt.Should().NotBeNull());
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }
}
