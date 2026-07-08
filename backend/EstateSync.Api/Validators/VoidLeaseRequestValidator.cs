using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class VoidLeaseRequestValidator : AbstractValidator<VoidLeaseRequest>
{
    public VoidLeaseRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
