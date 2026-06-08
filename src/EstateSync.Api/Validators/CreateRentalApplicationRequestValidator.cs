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

        RuleFor(x => x.MoveInDate)
            .NotEmpty().WithMessage("Move-in date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Move-in date cannot be in the past.");

        RuleFor(x => x.MoveOutDate)
            .NotEmpty().WithMessage("Move-out date is required.")
            .GreaterThan(x => x.MoveInDate).WithMessage("Move-out date must be after the move-in date.");

        RuleFor(x => x.NegotiatedPrice)
            .GreaterThan(0).WithMessage("Negotiated price must be greater than zero.");

        RuleFor(x => x.CommuteDistance)
            .GreaterThanOrEqualTo(0).WithMessage("Commute distance cannot be negative.");
    }
}
