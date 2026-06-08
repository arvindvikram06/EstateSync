using System;

namespace EstateSync.Api.Models;

public class SavedProperty
{
    public Guid SavedPropertyId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
}
