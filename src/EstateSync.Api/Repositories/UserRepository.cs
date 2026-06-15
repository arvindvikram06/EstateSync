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
        // Check if user has the Owner role by name (not hardcoded GUID)
        var hasOwnerRole = await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role != null && ur.Role.Name == "Owner");

        if (hasOwnerRole) return true;

        return false;
    }

    public async Task<User?> GetByIdWithRolesAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
}