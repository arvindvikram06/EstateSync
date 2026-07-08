using FluentValidation;
using EstateSync.Api.DTOs.Auth;

namespace EstateSync.Api.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
    }
}
