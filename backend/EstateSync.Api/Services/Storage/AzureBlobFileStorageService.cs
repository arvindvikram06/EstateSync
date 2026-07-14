using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Storage;
using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.Services.Storage;

public class AzureBlobFileStorageService : IFileStorageService
{
    private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
    private static readonly string[] ALLOWED_MIME_TYPES = ["image/jpeg", "image/png", "image/webp", "application/pdf"];

    private const string PUBLIC_CONTAINER = "estatesync-public";
    private const string PRIVATE_CONTAINER = "estatesync-private";

    // Subfolders that go into the private container (KYC docs, lease PDFs, signatures)
    private static readonly string[] PRIVATE_SUBFOLDERS = ["kyc", "leases", "signatures"];

    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobFileStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException("File is empty.");

        if (file.Length > MAX_FILE_SIZE_BYTES)
            throw new BadRequestException("File size exceeds the limit of 10MB.");

        if (!ALLOWED_MIME_TYPES.Contains(file.ContentType.ToLower()))
            throw new BadRequestException("Unsupported file type. Only JPEG, PNG, WEBP images, and PDF documents are allowed.");

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{subFolder}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        return await UploadAsync(stream, blobName, file.ContentType, subFolder, ct);
    }

    public async Task<string> SaveBytesAsync(byte[] bytes, string subFolder, string fileName, CancellationToken ct = default)
    {
        var blobName = $"{subFolder}/{fileName}";
        var contentType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "image/png";

        using var stream = new MemoryStream(bytes);
        return await UploadAsync(stream, blobName, contentType, subFolder, ct);
    }

    public async Task DeleteFileAsync(string blobPath)
    {
        if (string.IsNullOrEmpty(blobPath)) return;

        // blobPath format: "{containerName}/{blobName}"
        var slash = blobPath.IndexOf('/');
        if (slash < 0) return;

        var containerName = blobPath[..slash];
        var blobName = blobPath[(slash + 1)..];

        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.GetBlobClient(blobName).DeleteIfExistsAsync();
    }

    // Returns a viewable URL: SAS (1 hour) for private blobs, direct URL for public blobs.
    public Task<string> GetFileUrlAsync(string blobPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(blobPath)) return Task.FromResult(string.Empty);

        var slash = blobPath.IndexOf('/');
        if (slash < 0) return Task.FromResult(blobPath);

        var containerName = blobPath[..slash];
        var blobName = blobPath[(slash + 1)..];

        var blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (containerName == PRIVATE_CONTAINER)
        {
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            return Task.FromResult(sasUri.ToString());
        }

        return Task.FromResult(blobClient.Uri.ToString());
    }

    private async Task<string> UploadAsync(Stream stream, string blobName, string contentType, string subFolder, CancellationToken ct)
    {
        var isPrivate = PRIVATE_SUBFOLDERS.Any(p => subFolder.Contains(p, StringComparison.OrdinalIgnoreCase));
        var containerName = isPrivate ? PRIVATE_CONTAINER : PUBLIC_CONTAINER;

        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(
            publicAccessType: isPrivate ? PublicAccessType.None : PublicAccessType.Blob,
            cancellationToken: ct);

        await container.GetBlobClient(blobName).UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        // Return the blob path — NOT a URL. Stored in DB. Call GetFileUrlAsync to serve.
        return $"{containerName}/{blobName}";
    }
}
