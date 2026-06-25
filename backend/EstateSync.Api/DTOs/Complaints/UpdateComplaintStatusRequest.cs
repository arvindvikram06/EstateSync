using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Complaints;

public class UpdateComplaintStatusRequest
{
    public ComplaintStatus Status { get; set; }
    public string? ReviewRemarks { get; set; }
}
