using FluentValidation;
using EstateSync.Api.DTOs.OwnerVerification;

namespace EstateSync.Api.Validators;

public class SubmitOwnerVerificationRequestValidator : AbstractValidator<SubmitOwnerVerificationRequest>
{
    public SubmitOwnerVerificationRequestValidator()
    {
    }
}
