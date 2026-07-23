using System.Linq;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class ComplaintMapper
{
    public ComplaintResponse MapToResponse(Complaint complaint)
    {
        return new ComplaintResponse
        {
            ComplaintId = complaint.ComplaintId,
            LeaseId = complaint.LeaseId,
            PropertyTitle = complaint.Lease?.Property?.Title,
            Title = complaint.Title,
            Description = complaint.Description,
            ComplaintType = complaint.ComplaintType.ToString(),
            Priority = complaint.Priority.ToString(),
            Status = complaint.Status.ToString(),
            ReviewRemarks = complaint.ReviewRemarks,
            ResolutionSummary = complaint.ResolutionSummary,
            ResolutionProposedAt = complaint.ResolutionProposedAt,
            ResolvedAt = complaint.ResolvedAt,
            ReopenCount = complaint.ReopenCount,
            LastReopenedAt = complaint.LastReopenedAt,
            ReopenReason = complaint.ReopenReason,
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt,
            Images = complaint.ComplaintImages
                .Select(MapToImageResponse)
                .ToList(),
            Activities = complaint.Activities
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ComplaintActivityResponse
                {
                    ActivityId = x.ComplaintActivityId,
                    ActorName = x.ActorName,
                    ActorRole = x.ActorRole,
                    ActivityType = x.ActivityType,
                    PreviousStatus = x.PreviousStatus?.ToString(),
                    NewStatus = x.NewStatus?.ToString(),
                    Summary = x.Summary,
                    CreatedAt = x.CreatedAt
                }).ToList()
        };
    }

    public ComplaintImageResponse MapToImageResponse(ComplaintImage image)
    {
        return new ComplaintImageResponse
        {
            ComplaintImageId = image.ComplaintImageId,
            ImageUrl = image.Image
        };
    }
}
