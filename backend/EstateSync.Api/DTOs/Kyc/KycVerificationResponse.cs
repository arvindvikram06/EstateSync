using System;
using System.Collections.Generic;
using EstateSync.Api.DTOs.Users;

namespace EstateSync.Api.DTOs.Kyc;

public class KycVerificationResponse
{
    public Guid KycVerificationId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AadhaarNumber { get; set; }
    public string? PanNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DocumentResponse> Documents { get; set; } = new();
}
