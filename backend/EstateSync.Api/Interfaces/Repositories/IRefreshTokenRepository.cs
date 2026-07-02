using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetActiveTokensInFamilyAsync(string tokenFamily);

    /// <summary>
    /// Permanently removes refresh tokens that are no longer useful: those past their expiry,
    /// or revoked longer ago than <paramref name="revokedRetention"/>. Recently-revoked rows are
    /// kept so the rotation grace window and short-term audit still work. Returns rows deleted.
    /// </summary>
    Task<int> DeleteExpiredAndStaleRevokedAsync(DateTime now, TimeSpan revokedRetention);
}
