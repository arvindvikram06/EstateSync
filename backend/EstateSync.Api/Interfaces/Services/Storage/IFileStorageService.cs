using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.Interfaces.Services.Storage;

public interface IFileStorageService
{
    // Returns a blob path (e.g. "estatesync-private/kyc/guid.jpg") — store this in the DB.
    Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct);
    Task<string> SaveBytesAsync(byte[] bytes, string subFolder, string fileName, CancellationToken ct = default);
    Task DeleteFileAsync(string blobPath);

    // Converts a stored blob path into a viewable URL.
    // Azure: SAS URL (private) or direct URL (public). Local dev: returns the path as-is.
    Task<string> GetFileUrlAsync(string blobPath, CancellationToken ct = default);
}
