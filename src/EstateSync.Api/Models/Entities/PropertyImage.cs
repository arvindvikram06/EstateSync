using System;

namespace EstateSync.Api.Models;

public class PropertyImage
{
    public Guid ImageId { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public required string Image { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
