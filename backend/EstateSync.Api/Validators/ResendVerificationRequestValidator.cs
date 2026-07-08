using FluentValidation;
using EstateSync.Api.DTOs.Auth;

namespace EstateSync.Api.Validators;

public class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
