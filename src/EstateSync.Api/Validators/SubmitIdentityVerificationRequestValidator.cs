using FluentValidation;
using EstateSync.Api.DTOs.IdentityVerification;

namespace EstateSync.Api.Validators;

public class SubmitIdentityVerificationRequestValidator : AbstractValidator<SubmitIdentityVerificationRequest>
{
    public SubmitIdentityVerificationRequestValidator()
    {
        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Matches(@"^[0-9]{12}$").WithMessage("Aadhaar number must be exactly 12 digits.");

        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN number is required.")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$").WithMessage("PAN number must be in the format ABCDE1234F.");
    }
}
