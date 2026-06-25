using System.ComponentModel.DataAnnotations;

namespace EstateSync.Api.DTOs.Kyc;

public class SubmitKycRequest
{
    [Required]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar number must be exactly 12 digits.")]
    public string AadhaarNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "PAN must be in format ABCDE1234F.")]
    public string PanNumber { get; set; } = string.Empty;
}
