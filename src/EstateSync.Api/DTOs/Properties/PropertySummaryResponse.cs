using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Properties;

public record PropertySummaryResponse
{
    public Guid PropertyId { get; init; }
    public required string Title { get; init; }
    public decimal MonthlyRent { get; init; }
    public required string State { get; init; }
    public string? PrimaryImage { get; init; }
    public string? PropertyType { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public string? Status { get; init; }
    public Guid? AgreementTypeId { get; init; }
}
