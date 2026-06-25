using FluentValidation;
using EstateSync.Api.DTOs.Complaints;

namespace EstateSync.Api.Validators;

public class CreateComplaintRequestValidator : AbstractValidator<CreateComplaintRequest>
{
    public CreateComplaintRequestValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty().WithMessage("LeaseId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.ComplaintType)
            .IsInEnum().WithMessage("Invalid complaint type.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority.");
    }
}
