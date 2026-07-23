using EstateSync.Api.DTOs.SiteVisits;
using FluentValidation;

namespace EstateSync.Api.Validators;

public sealed class RescheduleSiteVisitRequestValidator : AbstractValidator<RescheduleSiteVisitRequest>
{
    public RescheduleSiteVisitRequestValidator()
    {
        RuleFor(x => x.NewSlotId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
