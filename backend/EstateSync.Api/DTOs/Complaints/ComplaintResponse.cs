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
    public string? PropertyTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTime? ResolutionProposedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int ReopenCount { get; set; }
    public DateTime? LastReopenedAt { get; set; }
    public string? ReopenReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ComplaintImageResponse> Images { get; set; } = new();
    public List<ComplaintActivityResponse> Activities { get; set; } = new();
}

public class ComplaintActivityResponse
{
    public Guid ActivityId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
}
