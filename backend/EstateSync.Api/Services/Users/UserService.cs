using System;
using EstateSync.Api.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Users;
using EstateSync.Api.Models;
using EstateSync.Api.Models.Enums;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService)
    {
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }

    public async Task<string> BecomeOwnerAsync(Guid userId, BecomeOwnerRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (!user.IsKycVerified)
            throw new ForbiddenException("You must complete KYC verification before becoming an owner.");

        var alreadyOwner = user.UserRoles.Any(ur => ur.Role?.Name == Roles.Owner);
        if (alreadyOwner)
            return "You are already an owner. Complete Stripe onboarding to start listing properties.";

        var ownerRole = await _roleRepo.GetByNameAsync(Roles.Owner)
            ?? throw new NotFoundException("Role", Roles.Owner);

        user.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = ownerRole.RoleId });

        user.OwnerProfile = new OwnerProfile
        {
            UserId = user.UserId,
            OwnerType = request.OwnerType,
            CompanyName = request.OwnerType == OwnerType.Company ? request.CompanyName : null,
            CompanyType = request.OwnerType == OwnerType.Company ? request.CompanyType : null,
            CIN = request.OwnerType == OwnerType.Company ? request.CIN : null,
            GSTIN = request.OwnerType == OwnerType.Company ? request.GSTIN : null,
            OwnerSince = DateTime.UtcNow,
        };

        await _unitOfWork.SaveChangesAsync(ct);

        return "Owner role granted. Complete Stripe onboarding to start listing properties.";
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        if (request.CurrentPassword == request.NewPassword)
            throw new BadRequestException("New password must be different from the current password.");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithProfileAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return new UserProfileResponse
        {
            FullName        = user.FullName,
            Email           = user.Email,
            PhoneNumber     = user.PhoneNumber,
            CreatedAt       = user.CreatedAt,
            IsKycVerified   = user.IsKycVerified,
            IsOwnerVerified = user.IsOwnerVerified,
            OwnerType       = user.OwnerProfile?.OwnerType.ToString(),
            CompanyName     = user.OwnerProfile?.CompanyName,
            CompanyType     = user.OwnerProfile?.CompanyType?.ToString(),
            GSTIN           = user.OwnerProfile?.GSTIN,
        };
    }
}
