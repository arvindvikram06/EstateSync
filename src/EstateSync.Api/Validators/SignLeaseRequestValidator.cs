using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class SignLeaseRequestValidator : AbstractValidator<SignLeaseRequest>
{
    public SignLeaseRequestValidator()
    {
        RuleFor(x => x.SignatureText)
            .NotEmpty().WithMessage("Signature text is required.")
            .MaximumLength(100).WithMessage("Signature text cannot exceed 100 characters.");
    }
}
