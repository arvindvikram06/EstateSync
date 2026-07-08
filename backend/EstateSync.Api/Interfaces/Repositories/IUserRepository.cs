using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailVerificationTokenAsync(string token);
    Task<User?> GetByEmailWithRolesAsync(string email);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<bool> IsUserVerifiedOwnerAsync(Guid userId);
    Task<User?> GetByIdWithRolesAsync(Guid userId);
    Task<User?> GetByIdWithProfileAsync(Guid userId);
}
