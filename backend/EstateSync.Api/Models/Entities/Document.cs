using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Document : BaseEntity
{
    public Guid DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public required string DocumentUrl { get; set; }
    public required string OriginalFileName { get; set; }
    public required string MimeType { get; set; }
    public long FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public ApprovalStatus ReviewStatus { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}
