using System;
using EstateSync.Api.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Admin;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Admin;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPasswordService _passwordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILeaseService _leaseService;

    public AdminService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IPasswordService passwordService,
        IUnitOfWork unitOfWork,
        ILeaseService leaseService)
    {
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _leaseService = leaseService ?? throw new ArgumentNullException(nameof(leaseService));
    }

    public async Task CreateVerifierAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new ConflictException("A user with this email already exists.");

        var verifierRole = await _roleRepo.GetByNameAsync(Roles.Verifier)
            ?? throw new InvalidOperationException("Verifier role not seeded. Please contact support.");

        var verifier = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _passwordService.HashPassword(request.Password),
            IsActive = true,
            IsKycVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        verifier.UserRoles.Add(new UserRole
        {
            UserId = verifier.UserId,
            RoleId = verifierRole.RoleId
        });

        await _userRepo.AddAsync(verifier);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeactivateStaffAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var isVerifier = user.UserRoles.Any(ur => ur.Role?.Name == Roles.Verifier);
        if (!isVerifier)
            throw new BadRequestException("This user is not a verifier.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task ApproveLeaseAsync(Guid leaseId, bool approve, string? remarks, Guid adminId, CancellationToken ct = default)
        => _leaseService.AdminApproveLeaseAsync(leaseId, approve, remarks, adminId, ct);

    public async Task<IEnumerable<StaffResponse>> GetStaffAsync(CancellationToken ct = default)
    {
        var verifiers = await _userRepo.GetUsersByRoleAsync(Roles.Verifier);
        return verifiers.Select(u => new StaffResponse(
            u.UserId, u.FullName, u.Email, u.PhoneNumber, u.IsActive, u.CreatedAt));
    }
}
