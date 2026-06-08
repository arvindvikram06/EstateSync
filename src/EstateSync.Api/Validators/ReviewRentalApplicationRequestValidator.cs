using FluentValidation;
using EstateSync.Api.DTOs.RentalApplications;

namespace EstateSync.Api.Validators;

public class ReviewRentalApplicationRequestValidator : AbstractValidator<ReviewRentalApplicationRequest>
{
    public ReviewRentalApplicationRequestValidator()
    {
        RuleFor(x => x.Remarks)
            .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters.");
    }
}
