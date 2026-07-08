using FluentValidation;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models.Enums;

namespace EstateSync.Api.Validators;

public class BecomeOwnerRequestValidator : AbstractValidator<BecomeOwnerRequest>
{
    public BecomeOwnerRequestValidator()
    {
        When(x => x.OwnerType == OwnerType.Company, () =>
        {
            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Company name is required for company owners")
                .MaximumLength(300).WithMessage("Company name must not exceed 300 characters");

            RuleFor(x => x.CompanyType)
                .NotNull().WithMessage("Company type is required for company owners");

            RuleFor(x => x.GSTIN)
                .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
                .When(x => !string.IsNullOrWhiteSpace(x.GSTIN))
                .WithMessage("GSTIN must be a valid 15-character GST number");

            RuleFor(x => x.CIN)
                .MaximumLength(21).WithMessage("CIN must not exceed 21 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.CIN));
        });
    }
}
