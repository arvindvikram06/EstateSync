using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class UpdateLeaseRequestValidator : AbstractValidator<UpdateLeaseRequest>
{
    public UpdateLeaseRequestValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate!.Value).WithMessage("End date must be after the start date.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.MonthlyRent)
            .GreaterThan(0).WithMessage("Monthly rent must be greater than zero.")
            .When(x => x.MonthlyRent.HasValue);

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Security deposit cannot be negative.")
            .When(x => x.SecurityDeposit.HasValue);

        RuleFor(x => x.PaymentFrequency)
            .IsInEnum().WithMessage("A valid payment frequency is required.")
            .When(x => x.PaymentFrequency.HasValue);

        RuleFor(x => x.NoticePeriodDays)
            .InclusiveBetween(0, 365).WithMessage("Notice period must be between 0 and 365 days.")
            .When(x => x.NoticePeriodDays.HasValue);

        RuleFor(x => x.PenaltyPercentage)
            .InclusiveBetween(0, 100).WithMessage("Penalty percentage must be between 0 and 100.")
            .When(x => x.PenaltyPercentage.HasValue);

        RuleFor(x => x.AgreementText)
            .MaximumLength(20000).WithMessage("Agreement text must not exceed 20000 characters.")
            .When(x => x.AgreementText != null);
    }
}
