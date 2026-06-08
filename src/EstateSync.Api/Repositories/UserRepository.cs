using System;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetWithRolesByEmailAsync(string email)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<System.Collections.Generic.IEnumerable<User>> GetUsersByRoleAsync(string roleName)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == roleName))
            .ToListAsync();
    }

    public async Task<bool> IsUserVerifiedOwnerAsync(Guid userId)
    {
        // First check if user is already assigned the Owner role
        var hasOwnerRole = await _context.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == Guid.Parse("22222222-2222-2222-2222-222222222222"));

        if (hasOwnerRole) return true;

        // If not, check if they have an approved OwnerVerificationRequest
        return await _context.Set<OwnerVerificationRequest>()
            .AnyAsync(r => r.UserId == userId && r.Status == ApprovalStatus.Approved);
    }

    public async Task<User?> GetWithRolesByIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
}