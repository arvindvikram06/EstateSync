using System;

namespace EstateSync.Api.Models;

public class ComplaintImage
{
    public Guid ComplaintImageId { get; set; }
    public Guid ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public required string Image { get; set; }
    public DateTime CreatedAt { get; set; }
}
