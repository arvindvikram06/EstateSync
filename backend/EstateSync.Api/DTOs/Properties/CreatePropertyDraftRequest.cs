using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Properties;

public record CreatePropertyDraftRequest
{
    public PropertyType PropertyType { get; init; }
}
