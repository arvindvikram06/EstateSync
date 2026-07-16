using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Api.Services.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadRootPath;
    private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10MB
    private static readonly string[] ALLOWED_MIME_TYPES = { "image/jpeg", "image/png", "image/webp", "application/pdf" };

    public LocalFileStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:LocalUploadPath"];
        _uploadRootPath = !string.IsNullOrEmpty(configuredPath)
            ? Path.Combine(env.ContentRootPath, configuredPath)
            : Path.Combine(env.ContentRootPath, "uploads");
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException("File is empty.");

        if (file.Length > MAX_FILE_SIZE_BYTES)
            throw new BadRequestException("File size exceeds the limit of 10MB.");

        if (!ALLOWED_MIME_TYPES.Contains(file.ContentType.ToLower()))
            throw new BadRequestException("Unsupported file type. Only JPEG, PNG, WEBP images, and PDF documents are allowed.");

        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        var targetDirectory = Path.Combine(_uploadRootPath, subFolder);
        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, uniqueFileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var normalizedSubFolder = subFolder.Replace(Path.DirectorySeparatorChar, '/');
        return $"/uploads/{normalizedSubFolder}/{uniqueFileName}";
    }

    public async Task<string> SaveBytesAsync(byte[] bytes, string subFolder, string fileName, CancellationToken ct = default)
    {
        var targetDirectory = Path.Combine(_uploadRootPath, subFolder);
        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, bytes, ct);

        var normalizedSubFolder = subFolder.Replace(Path.DirectorySeparatorChar, '/');
        return $"/uploads/{normalizedSubFolder}/{fileName}";
    }

    public Task DeleteFileAsync(string blobPath)
    {
        if (string.IsNullOrEmpty(blobPath)) return Task.CompletedTask;

        if (blobPath.StartsWith("/uploads/"))
        {
            var relativePath = blobPath.Substring("/uploads/".Length).Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_uploadRootPath, relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    // Local dev: the stored path is already a usable relative URL (/uploads/...).
    public Task<string> GetFileUrlAsync(string blobPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(blobPath)) return Task.FromResult(string.Empty);
        if (blobPath.StartsWith("https://") || blobPath.StartsWith("http://")) return Task.FromResult(blobPath);
        return Task.FromResult(blobPath);
    }
}
