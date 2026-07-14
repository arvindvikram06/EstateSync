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
            .Matches(@"^(\+91|0)?[6-9]\d{9}$").WithMessage("Enter a valid Indian mobile number (e.g. 9876543210 or +919876543210)");
    }
}
