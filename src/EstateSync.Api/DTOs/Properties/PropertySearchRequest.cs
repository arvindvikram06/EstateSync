using System;

namespace EstateSync.Api.DTOs.Properties;

public record PropertySearchRequest
{
    public string? State { get; init; }
    public string? ListingType { get; init; }
    public string? PropertyType { get; init; }
    public int? Bedrooms { get; init; }
    public decimal? MinRent { get; init; }
    public decimal? MaxRent { get; init; }
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;
}
