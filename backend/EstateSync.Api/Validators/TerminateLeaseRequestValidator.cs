using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class TerminateLeaseRequestValidator : AbstractValidator<TerminateLeaseRequest>
{
    public TerminateLeaseRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Termination reason is required.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
