using FluentValidation;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Validators;

public class PropertyRequestBaseValidator<T> : AbstractValidator<T> where T : PropertyRequestBase
{
    public PropertyRequestBaseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("Location is required.");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Address line is required.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.MonthlyRent)
            .GreaterThan(0).WithMessage("Monthly rent must be greater than 0.");

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Security deposit must be greater than or equal to 0.");

        RuleFor(x => x.Bedrooms)
            .GreaterThan(0).WithMessage("Bedrooms must be greater than 0.");

        RuleFor(x => x.Bathrooms)
            .GreaterThan(0).WithMessage("Bathrooms must be greater than 0.");

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type.");
    }
}
