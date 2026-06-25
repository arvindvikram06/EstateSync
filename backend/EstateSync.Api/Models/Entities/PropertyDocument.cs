using System;

namespace EstateSync.Api.Models;

public class PropertyDocument
{
    public Guid PropertyDocumentId { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public DateTime CreatedAt { get; set; }
}
