using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class TenantFinancialProfile : BaseEntity
{
    public Guid ProfileId { get; set; }
    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }

    public EmploymentType EmploymentType { get; set; }
    public string? EmployerName { get; set; }
    public decimal DeclaredMonthlyIncome { get; set; }

    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public ICollection<TenantFinancialDocument> TenantFinancialDocuments { get; set; } = new List<TenantFinancialDocument>();
}
