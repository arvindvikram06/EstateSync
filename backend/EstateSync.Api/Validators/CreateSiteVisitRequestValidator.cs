using System;
using FluentValidation;
using EstateSync.Api.DTOs.SiteVisits;

namespace EstateSync.Api.Validators;

public class CreateSiteVisitRequestValidator : AbstractValidator<CreateSiteVisitRequest>
{
    public CreateSiteVisitRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("PropertyId is required.");

        RuleFor(x => x.VisitDate)
            .NotEmpty().WithMessage("Visit date is required.")
            .Must(date => date.Date >= DateTime.UtcNow.Date).WithMessage("Visit date cannot be in the past.")
            .Must(date => date.Date <= DateTime.UtcNow.Date.AddDays(60)).WithMessage("Visit date cannot be more than 60 days in the future.");

        RuleFor(x => x.VisitTime)
            .NotEmpty().WithMessage("Visit time is required.");
    }
}
