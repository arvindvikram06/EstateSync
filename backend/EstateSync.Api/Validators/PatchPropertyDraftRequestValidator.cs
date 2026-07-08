using FluentValidation;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Validators;

public class PatchPropertyDraftRequestValidator : AbstractValidator<PatchPropertyDraftRequest>
{
    public PatchPropertyDraftRequestValidator()
    {
        // Each rule fires only when the field is provided (non-null)

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type.")
            .When(x => x.PropertyType.HasValue);

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State cannot be empty.")
            .MaximumLength(150).WithMessage("State cannot exceed 150 characters.")
            .When(x => x.State != null);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City cannot be empty.")
            .MaximumLength(150).WithMessage("City cannot exceed 150 characters.")
            .When(x => x.City != null);

        RuleFor(x => x.Pincode)
            .NotEmpty().WithMessage("Pincode cannot be empty.")
            .Matches(@"^[1-9][0-9]{5}$").WithMessage("Pincode must be a valid 6-digit Indian pincode.")
            .When(x => x.Pincode != null);

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Address line cannot be empty.")
            .MaximumLength(500).WithMessage("Address line cannot exceed 500 characters.")
            .When(x => x.AddressLine != null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Bedrooms)
            .GreaterThan(0).WithMessage("Bedrooms must be greater than 0.")
            .When(x => x.Bedrooms.HasValue);

        RuleFor(x => x.Bathrooms)
            .GreaterThan(0).WithMessage("Bathrooms must be greater than 0.")
            .When(x => x.Bathrooms.HasValue);

        RuleFor(x => x.LandArea)
            .GreaterThan(0).WithMessage("Land area must be greater than 0.")
            .When(x => x.LandArea.HasValue);

        RuleFor(x => x.BuildupArea)
            .GreaterThan(0).WithMessage("Buildup area must be greater than 0.")
            .When(x => x.BuildupArea.HasValue);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description cannot be empty.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.MonthlyRent)
            .GreaterThan(0).WithMessage("Monthly rent must be greater than 0.")
            .When(x => x.MonthlyRent.HasValue);

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Security deposit must be greater than or equal to 0.")
            .When(x => x.SecurityDeposit.HasValue);
    }
}
