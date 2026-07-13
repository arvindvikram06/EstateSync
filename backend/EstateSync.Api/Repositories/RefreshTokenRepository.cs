using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensInFamilyAsync(string tokenFamily)
    {
        return await _dbSet
            .Where(rt => rt.TokenFamily == tokenFamily && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<int> DeleteExpiredAndStaleRevokedAsync(DateTime now, TimeSpan revokedRetention)
    {
        var revokedCutoff = now - revokedRetention;

        return await _dbSet
            .Where(rt => rt.ExpiresAt < now ||
                         (rt.RevokedAt != null && rt.RevokedAt < revokedCutoff))
            .ExecuteDeleteAsync();
    }
}