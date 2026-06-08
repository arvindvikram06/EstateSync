using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.Interfaces.Services.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct);
    Task DeleteFileAsync(string fileUrl);
}
