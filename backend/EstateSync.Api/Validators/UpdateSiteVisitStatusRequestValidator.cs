using FluentValidation;
using EstateSync.Api.DTOs.SiteVisits;

namespace EstateSync.Api.Validators;

public class UpdateSiteVisitStatusRequestValidator : AbstractValidator<UpdateSiteVisitStatusRequest>
{
    public UpdateSiteVisitStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid site visit status.");

        RuleFor(x => x.OwnerNotes)
            .MaximumLength(500).WithMessage("Owner notes must not exceed 500 characters.");
    }
}
