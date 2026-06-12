using FluentValidation;
using EstateSync.Api.DTOs.OwnerVerification;

namespace EstateSync.Api.Validators;

public class SubmitOwnerVerificationRequestValidator : AbstractValidator<SubmitOwnerVerificationRequest>
{
    public SubmitOwnerVerificationRequestValidator()
    {
        RuleFor(x => x.BankAccountNumber)
            .NotEmpty().WithMessage("Bank account number is required")
            .MaximumLength(100).WithMessage("Bank account number must not exceed 100 characters");

        RuleFor(x => x.BankIFSCCode)
            .NotEmpty().WithMessage("Bank IFSC code is required")
            .MaximumLength(50).WithMessage("Bank IFSC code must not exceed 50 characters");

        RuleFor(x => x.BankAccountHolderName)
            .NotEmpty().WithMessage("Bank account holder name is required")
            .MaximumLength(250).WithMessage("Bank account holder name must not exceed 250 characters");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required")
            .MaximumLength(250).WithMessage("Bank name must not exceed 250 characters");
    }
}
