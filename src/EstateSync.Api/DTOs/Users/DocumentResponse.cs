using System;

namespace EstateSync.Api.DTOs.Users;

public record DocumentResponse
{
    public Guid DocumentId { get; init; }
    public required string DocumentType { get; init; }
    public required string DocumentUrl { get; init; }
    public required string OriginalFileName { get; init; }
    public required string MimeType { get; init; }
    public long FileSize { get; init; }
    public required string ReviewStatus { get; init; }
    public DateTime UploadedAt { get; init; }
}
