using System;

namespace EstateSync.Api.Models;

public class IdentityVerificationDocument
{
    public Guid IdentityVerificationDocumentId { get; set; }
    
    public Guid RequestId { get; set; }
    public IdentityVerificationRequest? IdentityVerificationRequest { get; set; }
    
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
