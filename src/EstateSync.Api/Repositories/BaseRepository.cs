using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }


    public virtual async Task<(IEnumerable<T> Items, int Total)> GetPagedAsync(int page, int limit)
    {
        IQueryable<T> query = _dbSet;
        var keyName = _context.Model.FindEntityType(typeof(T))
            ?.FindPrimaryKey()
            ?.Properties
            .Select(x => x.Name)
            .FirstOrDefault();

        if (keyName != null)
        {
            query = query.OrderBy(x => EF.Property<object>(x!, keyName));
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    /// <summary>
    /// Only call this for DETACHED entities (e.g. loaded with AsNoTracking).
    /// For already-tracked entities, just mutate the properties directly — EF Core's
    /// change tracker detects changes automatically on SaveChangesAsync.
    /// Calling Update() on a tracked entity marks the entire graph as Modified,
    /// causing spurious UPDATEs on navigation properties and concurrency errors.
    /// </summary>
    public virtual Task UpdateAsync(T entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            _dbSet.Update(entity);
        }
        // If already tracked (Added/Modified/Unchanged), EF detects property changes automatically.
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
