using System;
using EstateSync.Api.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Auth;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Tokens;

using EstateSync.Api.Interfaces.Services.Email;
using EstateSync.Api.Services.Email;
namespace EstateSync.Api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EstateSync.Api.Mappers.AuthMapper _authMapper;
    private readonly IEmailQueue _emailQueue;
    private readonly IPasswordResetTokenStore _passwordResetTokenStore;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        EstateSync.Api.Mappers.AuthMapper authMapper,
        IEmailQueue emailQueue,
        IPasswordResetTokenStore passwordResetTokenStore)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _authMapper = authMapper ?? throw new ArgumentNullException(nameof(authMapper));
        _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
        _passwordResetTokenStore = passwordResetTokenStore ?? throw new ArgumentNullException(nameof(passwordResetTokenStore));
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var tenantRole = await _roleRepository.GetByNameAsync(Roles.Tenant);
        if (tenantRole == null)
        {
            throw new NotFoundException("Role", Roles.Tenant);
        }

        var newUser = CreateTenantUser(request, tenantRole);
        newUser.IsEmailVerified = false;
        IssueVerificationToken(newUser);
        await _userRepository.AddAsync(newUser);
        await _unitOfWork.SaveChangesAsync();

        // No tokens issued here — the user signs in after verifying their email.
        await QueueVerificationEmailAsync(newUser);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await _userRepository.GetByEmailVerificationTokenAsync(token);
        if (user == null)
            throw new BadRequestException("Invalid or already-used verification link.");

        if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("This verification link has expired. Please request a new one.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResendVerificationAsync(string email)
    {
        // Always succeed silently — never reveal whether an email is registered.
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsEmailVerified) return;

        IssueVerificationToken(user);
        await _unitOfWork.SaveChangesAsync();
        await QueueVerificationEmailAsync(user);
    }

    private static void IssueVerificationToken(User user)
    {
        user.EmailVerificationToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
    }

    private async Task QueueVerificationEmailAsync(User user)
    {
        await _emailQueue.EnqueueAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Verify your EstateSync email",
            Heading = $"Welcome, {user.FullName}!",
            Body = "Confirm your email address to activate your EstateSync account. This link is valid for 24 hours.",
            CtaLabel = "Verify my email",
            CtaUrl = $"http://localhost:4200/auth/verify-email?token={user.EmailVerificationToken}",
        });
    }

    public async Task<(AuthResponse Auth, string RefreshToken)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailWithRolesAsync(request.Email);
        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Your account is deactivated.");
        }

        if (!user.IsEmailVerified)
        {
            throw new ForbiddenException("Please verify your email before signing in. Check your inbox for the verification link.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            TokenFamily = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        user.LastLogin = DateTime.UtcNow;
        // await _userRepository.UpdateAsync(user);

        await _unitOfWork.SaveChangesAsync();

        var authResponse = _authMapper.MapToAuthResponse(user, accessToken);

        return (authResponse, refreshTokenString);
    }

    // Tokens rotate on every refresh. The SPA refreshes on each page load, so quick
    // or overlapping reloads can present a token that was rotated milliseconds earlier
    // (the new cookie not yet committed by the browser). Within this grace window we
    // treat that as a benign duplicate and return the family's current successor instead
    // of revoking the whole family. A genuine replay later still trips reuse detection.
    private static readonly TimeSpan RefreshRotationGrace = TimeSpan.FromSeconds(60);

    public async Task<(AuthResponse Auth, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (tokenEntity.RevokedAt != null)
        {
            // Benign concurrent/duplicate refresh: token was rotated very recently and a
            // valid successor still exists in the family — hand that back, don't nuke.
            if (DateTime.UtcNow - tokenEntity.RevokedAt.Value <= RefreshRotationGrace)
            {
                var activeSuccessor = (await _refreshTokenRepository
                        .GetActiveTokensInFamilyAsync(tokenEntity.TokenFamily))
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();

                if (activeSuccessor != null)
                {
                    var successorUser = await GetAndVerifyUserForRefreshAsync(activeSuccessor.UserId);
                    var successorAccessToken = _tokenService.GenerateAccessToken(successorUser);
                    return (_authMapper.MapToAuthResponse(successorUser, successorAccessToken), activeSuccessor.Token);
                }
            }

            // Outside the grace window, or no live successor — real reuse: revoke the family.
            await RevokeTokenFamilyAsync(tokenEntity.TokenFamily);
            await _unitOfWork.SaveChangesAsync();
            throw new UnauthorizedException("Token family reuse detected. Please log in again.");
        }

        if (tokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        tokenEntity.RevokedAt = DateTime.UtcNow;
        // await _refreshTokenRepository.UpdateAsync(tokenEntity);

        var user = await GetAndVerifyUserForRefreshAsync(tokenEntity.UserId);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenString = _tokenService.GenerateRefreshToken();

        await CreateAndSaveRefreshTokenAsync(user.UserId, newRefreshTokenString, tokenEntity.TokenFamily);
        await _unitOfWork.SaveChangesAsync();

        var authResponse = _authMapper.MapToAuthResponse(user, newAccessToken);
        return (authResponse, newRefreshTokenString);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        // Idempotent: a missing/unknown token is a no-op so logout never fails.
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null)
        {
            return;
        }

        // Revoke the whole family so the session is truly ended server-side.
        await RevokeTokenFamilyAsync(tokenEntity.TokenFamily);
        await _unitOfWork.SaveChangesAsync();
    }

    private static readonly TimeSpan PasswordResetTokenTtl = TimeSpan.FromHours(1);

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Always succeed silently — never reveal whether an email is registered.
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive) return;

        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await _passwordResetTokenStore.StoreAsync(token, user.UserId, PasswordResetTokenTtl);

        await _emailQueue.EnqueueAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Reset your EstateSync password",
            Heading = $"Hi {user.FullName},",
            Body = "We received a request to reset your password. Click the button below to choose a new one. " +
                   "This link is valid for 1 hour. If you didn't request this, you can safely ignore this email.",
            CtaLabel = "Reset Password",
            CtaUrl = $"http://localhost:4200/auth/reset-password?token={token}",
        });
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var userId = await _passwordResetTokenStore.ConsumeAsync(request.Token);
        if (userId == null)
            throw new BadRequestException("Invalid or expired reset link. Please request a new one.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null || !user.IsActive)
            throw new BadRequestException("Invalid or expired reset link. Please request a new one.");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);

        var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(user.UserId);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private User CreateTenantUser(RegisterRequest request, Role tenantRole)
    {
        var passwordHash = _passwordService.HashPassword(request.Password);

        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = passwordHash,
            IsActive = true
        };

        newUser.UserRoles.Add(new UserRole
        {
            // Do NOT generate UserRoleId manually
            UserId = newUser.UserId,
            RoleId = tenantRole.RoleId,
            Role = tenantRole
        });

        return newUser;
    }

    private async Task CreateAndSaveRefreshTokenAsync(Guid userId, string tokenString, string tokenFamily)
    {
        var refreshTokenEntity = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            TokenFamily = tokenFamily,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
    }

    private async Task RevokeTokenFamilyAsync(string tokenFamily)
    {
        var familyTokens = await _refreshTokenRepository.GetActiveTokensInFamilyAsync(tokenFamily);
        foreach (var token in familyTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    private async Task<User> GetAndVerifyUserForRefreshAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive.");
        }

        var detailedUser = await _userRepository.GetByEmailWithRolesAsync(user.Email);
        if (detailedUser == null)
        {
            throw new UnauthorizedException("User details not found.");
        }

        return detailedUser;
    }
}
