using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Properties;

public record PropertyDetailResponse
{
    public Guid PropertyId { get; init; }
    public Guid OwnerId { get; init; }
    public required string OwnerName { get; init; }
    public Guid LocationId { get; init; }
    public required string State { get; init; }
    public required string District { get; init; }
    public required string Pincode { get; init; }
    public required string Country { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string PropertyType { get; init; }
    public required string Status { get; init; }
    public required string AddressLine { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal LandArea { get; init; }
    public decimal BuildupArea { get; init; }
    public decimal MonthlyRent { get; init; }
    public decimal SecurityDeposit { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public string? ApproveRemarks { get; init; }
    public List<string> Amenities { get; init; } = new();
    public List<PropertyImageResponse> Images { get; init; } = new();
}

public record PropertyImageResponse
{
    public Guid ImageId { get; init; }
    public required string Image { get; init; }
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}
