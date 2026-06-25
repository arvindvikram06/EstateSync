using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Complaints;

public class ComplaintImageResponse
{
    public Guid ComplaintImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class ComplaintResponse
{
    public Guid ComplaintId { get; set; }
    public Guid LeaseId { get; set; }
    public Guid? LeaseBillId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ComplaintImageResponse> Images { get; set; } = new();
}
