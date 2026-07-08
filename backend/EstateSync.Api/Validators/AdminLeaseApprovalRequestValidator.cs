using FluentValidation;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Validators;

public class AdminLeaseApprovalRequestValidator : AbstractValidator<AdminLeaseApprovalRequest>
{
    public AdminLeaseApprovalRequestValidator()
    {
        RuleFor(x => x.Remarks)
            .MaximumLength(1000).WithMessage("Remarks must not exceed 1000 characters.");

        RuleFor(x => x.Remarks)
            .NotEmpty().WithMessage("Remarks are required when rejecting a lease.")
            .When(x => !x.Approve);
    }
}
