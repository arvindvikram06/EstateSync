using System;

namespace EstateSync.Api.Models;

public class TenantVerificationDocument
{
    public Guid TenantVerificationDocumentId { get; set; }
    public Guid RequestId { get; set; }
    public TenantVerificationRequest? TenantVerificationRequest { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public DateTime CreatedAt { get; set; }
}
