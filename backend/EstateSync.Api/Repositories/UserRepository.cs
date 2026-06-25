using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailWithRolesAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == roleName))
            .ToListAsync();
    }

    public async Task<bool> IsUserVerifiedOwnerAsync(Guid userId)
    {
        var user = await _dbSet
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return false;

        var hasOwnerRole = user.UserRoles.Any(ur => ur.Role?.Name == "Owner");
        return hasOwnerRole && user.IsKycVerified && user.IsOwnerVerified;
    }

    public async Task<User?> GetByIdWithRolesAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> IsAadhaarInUseAsync(string aadhaarNumber, Guid? excludeUserId = null)
    {
        return await _dbSet.AnyAsync(u => u.AadhaarNumber == aadhaarNumber && u.UserId != excludeUserId);
    }

    public async Task<bool> IsPanInUseAsync(string panNumber, Guid? excludeUserId = null)
    {
        return await _dbSet.AnyAsync(u => u.PanNumber == panNumber && u.UserId != excludeUserId);
    }
}