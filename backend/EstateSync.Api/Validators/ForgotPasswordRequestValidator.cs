using FluentValidation;
using EstateSync.Api.DTOs.Auth;

namespace EstateSync.Api.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("A valid email address is required")
            .MaximumLength(250).WithMessage("Email must not exceed 250 characters");
    }
}
