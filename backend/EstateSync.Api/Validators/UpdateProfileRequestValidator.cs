using FluentValidation;
using EstateSync.Api.DTOs.Users;

namespace EstateSync.Api.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters long")
            .MaximumLength(250).WithMessage("Full name must not exceed 250 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters");
    }
}
