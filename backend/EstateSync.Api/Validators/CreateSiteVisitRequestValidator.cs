using FluentValidation;
using EstateSync.Api.DTOs.SiteVisits;

namespace EstateSync.Api.Validators;

public class CreateSiteVisitRequestValidator : AbstractValidator<CreateSiteVisitRequest>
{
    public CreateSiteVisitRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("PropertyId is required.");

        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("An available visit slot is required.");
    }
}
