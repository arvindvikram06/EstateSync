using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Complaints;

public class CreateComplaintRequest
{
    public Guid LeaseId { get; set; }
    public Guid? LeaseBillId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ComplaintType ComplaintType { get; set; }
    public ComplaintPriority Priority { get; set; }
}
