using System;
using System.Threading.Tasks;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default);
}
