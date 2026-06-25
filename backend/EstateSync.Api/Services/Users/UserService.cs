using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Users;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepo, IRoleRepository roleRepo, IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<string> BecomeOwnerAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (!user.IsKycVerified)
            throw new ForbiddenException("You must complete KYC verification before becoming an owner.");

        var alreadyOwner = user.UserRoles.Any(ur => ur.Role?.Name == "Owner");
        if (alreadyOwner)
            return "You are already registered as an owner.";

        var ownerRole = await _roleRepo.GetByNameAsync("Owner")
            ?? throw new BadRequestException("Owner role not found. Please contact support.");

        user.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = ownerRole.RoleId
        });

        await _unitOfWork.SaveChangesAsync(ct);

        return "Owner role assigned successfully. Please complete Stripe onboarding to start listing properties.";
    }
}
