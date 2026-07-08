using FluentValidation;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Validators;

public class CreatePropertyDraftRequestValidator : AbstractValidator<CreatePropertyDraftRequest>
{
    public CreatePropertyDraftRequestValidator()
    {
        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type.");
    }
}
