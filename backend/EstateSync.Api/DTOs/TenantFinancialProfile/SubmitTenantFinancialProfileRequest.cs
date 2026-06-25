using System.ComponentModel.DataAnnotations;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.TenantFinancialProfile;

public class SubmitTenantFinancialProfileRequest
{
    [Required]
    public EmploymentType EmploymentType { get; set; }

    [MaxLength(250)]
    public string? EmployerName { get; set; }

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Monthly income must be greater than zero.")]
    public decimal DeclaredMonthlyIncome { get; set; }
}
