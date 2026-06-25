using System;
using System.Collections.Generic;
using EstateSync.Api.DTOs.Users;

namespace EstateSync.Api.DTOs.TenantFinancialProfile;

public class TenantFinancialProfileResponse
{
    public Guid ProfileId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string? EmployerName { get; set; }
    public decimal DeclaredMonthlyIncome { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewRemarks { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DocumentResponse> Documents { get; set; } = new();
}
