using FluentValidation;
using EstateSync.Api.DTOs.Complaints;

namespace EstateSync.Api.Validators;

public class UpdateComplaintStatusRequestValidator : AbstractValidator<UpdateComplaintStatusRequest>
{
    public UpdateComplaintStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid complaint status is required.");

        RuleFor(x => x.ReviewRemarks)
            .MaximumLength(1000).WithMessage("Remarks must not exceed 1000 characters.");
    }
}
