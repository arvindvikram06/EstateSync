using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class ApproveRenewalRequestValidator : AbstractValidator<ApproveRenewalRequest>
{
    public ApproveRenewalRequestValidator()
    {
        RuleFor(x => x.NewEndDate)
            .NotEmpty().WithMessage("New end date is required.");
    }
}
