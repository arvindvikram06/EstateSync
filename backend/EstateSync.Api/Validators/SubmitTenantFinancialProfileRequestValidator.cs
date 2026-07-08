using FluentValidation;
using EstateSync.Api.DTOs.TenantFinancialProfile;

namespace EstateSync.Api.Validators;

public class SubmitTenantFinancialProfileRequestValidator : AbstractValidator<SubmitTenantFinancialProfileRequest>
{
    public SubmitTenantFinancialProfileRequestValidator()
    {
        RuleFor(x => x.EmploymentType)
            .IsInEnum().WithMessage("A valid employment type is required.");

        RuleFor(x => x.EmployerName)
            .MaximumLength(250).WithMessage("Employer name must not exceed 250 characters.");

        RuleFor(x => x.DeclaredMonthlyIncome)
            .GreaterThan(0).WithMessage("Monthly income must be greater than zero.");
    }
}
