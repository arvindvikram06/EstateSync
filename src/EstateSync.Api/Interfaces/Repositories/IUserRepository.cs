using System;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRolesByEmailAsync(string email);
    Task<System.Collections.Generic.IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<bool> IsUserVerifiedOwnerAsync(Guid userId);
    Task<User?> GetWithRolesByIdAsync(Guid userId);
}
