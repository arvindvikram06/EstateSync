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
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt,
            Images = complaint.ComplaintImages
                .Select(MapToImageResponse)
                .ToList()
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
