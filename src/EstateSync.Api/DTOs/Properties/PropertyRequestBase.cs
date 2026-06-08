using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Properties;

public abstract record PropertyRequestBase
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public PropertyType PropertyType { get; init; }
    public Guid LocationId { get; init; }
    public required string AddressLine { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal LandArea { get; init; }
    public decimal BuildupArea { get; init; }
    public decimal MonthlyRent { get; init; }
    public decimal SecurityDeposit { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public Guid? AgreementTypeId { get; init; }
}
