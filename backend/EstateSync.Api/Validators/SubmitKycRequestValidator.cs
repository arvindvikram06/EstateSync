using FluentValidation;
using EstateSync.Api.DTOs.Kyc;

namespace EstateSync.Api.Validators;

public class SubmitKycRequestValidator : AbstractValidator<SubmitKycRequest>
{
    public SubmitKycRequestValidator()
    {
        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Matches(@"^\d{12}$").WithMessage("Aadhaar number must be exactly 12 digits.");

        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN is required.")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$").WithMessage("PAN must be in format ABCDE1234F.");
    }
}
