using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.Interfaces.Services.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct);
    Task<string> SaveBytesAsync(byte[] bytes, string subFolder, string fileName, CancellationToken ct = default);
    Task DeleteFileAsync(string fileUrl);
}
