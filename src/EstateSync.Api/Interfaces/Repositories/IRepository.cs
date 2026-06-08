using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<(IEnumerable<T> Items, int Total)> GetAllAsync(int page, int limit);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
