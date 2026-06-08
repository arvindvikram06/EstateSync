using System;

namespace EstateSync.Api.Models;

public class OwnerVerificationDocument
{
    public Guid OwnerVerificationDocumentId { get; set; }
    public Guid RequestId { get; set; }
    public OwnerVerificationRequest? OwnerVerificationRequest { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public DateTime CreatedAt { get; set; }
}
