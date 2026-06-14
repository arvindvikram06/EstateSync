using FluentValidation;
using EstateSync.Api.DTOs.OwnerVerification;

namespace EstateSync.Api.Validators;

public class SubmitOwnerVerificationRequestValidator : AbstractValidator<SubmitOwnerVerificationRequest>
{
    public SubmitOwnerVerificationRequestValidator()
    {
        RuleFor(x => x.PanOrTaxId)
            .NotEmpty().WithMessage("PAN or Tax ID is required")
            .MaximumLength(50).WithMessage("PAN or Tax ID must not exceed 50 characters");
    }
}
