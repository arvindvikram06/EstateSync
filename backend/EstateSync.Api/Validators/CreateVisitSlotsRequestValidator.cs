using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Services.SiteVisits;
using FluentValidation;

namespace EstateSync.Api.Validators;

public sealed class CreateVisitSlotsRequestValidator : AbstractValidator<CreateVisitSlotsRequest>
{
    public CreateVisitSlotsRequestValidator()
    {
        RuleFor(x => x.Date)
            .Must(date => date.Date >= SiteVisitSlotRules.CurrentLocalDateTime().Date)
            .WithMessage("Slot date cannot be in the past.")
            .Must(date => date.Date <= SiteVisitSlotRules.CurrentLocalDateTime().Date.AddDays(60))
            .WithMessage("Slots can only be published 60 days ahead.");
        RuleFor(x => x.StartTimes).NotEmpty().Must(x => x.Count <= 12);
        RuleForEach(x => x.StartTimes)
            .Must(SiteVisitSlotRules.IsSupported)
            .WithMessage("Slots must start on a full hour between 09:00 and 18:00.");
    }
}
