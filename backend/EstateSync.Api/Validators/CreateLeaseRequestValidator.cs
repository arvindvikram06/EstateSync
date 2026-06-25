using System;
using FluentValidation;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Validators;

public class CreateLeaseRequestValidator : AbstractValidator<CreateLeaseRequest>
{
    public CreateLeaseRequestValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.MonthlyRent)
            .GreaterThan(0).WithMessage("Monthly rent must be greater than zero.");

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Security deposit cannot be negative.");

        RuleFor(x => x.NoticePeriodDays)
            .GreaterThanOrEqualTo(0).WithMessage("Notice period days cannot be negative.")
            .When(x => x.NoticePeriodDays.HasValue);

        RuleFor(x => x.PenaltyPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Penalty percentage cannot be negative.")
            .When(x => x.PenaltyPercentage.HasValue);
    }
}
