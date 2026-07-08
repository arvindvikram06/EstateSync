namespace EstateSync.Api.Services.Email;

public record EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Heading { get; init; }
    public required string Body { get; init; }
    public string? CtaLabel { get; init; }
    public string? CtaUrl { get; init; }
}
