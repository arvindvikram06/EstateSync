using FluentValidation;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Validators;

public class UpdatePropertyRequestValidator : PropertyRequestBaseValidator<UpdatePropertyRequest>
{
    public UpdatePropertyRequestValidator()
    {
    }
}
