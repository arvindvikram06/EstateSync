using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Ai;

public interface IAiService
{
    Task<string> ChatAsync(string message, CancellationToken ct = default);
}
