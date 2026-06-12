using System;
using FluentValidation;
using EstateSync.Api.DTOs.RentalApplications;

namespace EstateSync.Api.Validators;

public class CreateRentalApplicationRequestValidator : AbstractValidator<CreateRentalApplicationRequest>
{
    public CreateRentalApplicationRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");

        RuleFor(x => x.ExpectedMoveInDate)
            .NotEmpty().WithMessage("Expected move-in date is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Expected move-in date cannot be in the past.");

        RuleFor(x => x.DurationMonths)
            .GreaterThanOrEqualTo(11).WithMessage("Duration must be greater than 10 months.");

        RuleFor(x => x.CommuteDistance)
            .GreaterThanOrEqualTo(0).WithMessage("Commute distance cannot be negative.");
    }
}
