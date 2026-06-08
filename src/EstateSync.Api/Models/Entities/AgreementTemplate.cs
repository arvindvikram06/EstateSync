using System;

namespace EstateSync.Api.Models;

public class AgreementTemplate
{
    public Guid TemplateId { get; set; }
    public Guid AgreementTypeId { get; set; }
    public AgreementType? AgreementType { get; set; }
    public required string TemplateName { get; set; }
    public required string HtmlContent { get; set; }
    public string? PlaceholderSchema { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
