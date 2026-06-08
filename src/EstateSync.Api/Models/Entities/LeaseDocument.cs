using System;

namespace EstateSync.Api.Models;

public class LeaseDocument
{
    public Guid LeaseDocumentId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public DateTime CreatedAt { get; set; }
}
