using System.Collections.Generic;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Properties;

/// <summary>
/// All fields are optional — only non-null fields are applied to the draft.
/// Sent incrementally as the owner advances through the listing wizard steps.
/// </summary>
public record PatchPropertyDraftRequest
{
    public PropertyType? PropertyType { get; init; }

    // Step 2 — Location
    public string? State { get; init; }
    public string? City { get; init; }
    public string? Pincode { get; init; }
    public string? AddressLine { get; init; }

    // Step 3 — Map pin (optional — null means owner skipped)
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    // Step 4 — Rooms
    public int? Bedrooms { get; init; }
    public int? Bathrooms { get; init; }

    // Step 5 — Area
    public decimal? LandArea { get; init; }
    public decimal? BuildupArea { get; init; }

    // Step 6 — Title
    public string? Title { get; init; }

    // Step 7 — Description
    public string? Description { get; init; }

    // Step 8 — Pricing
    public decimal? MonthlyRent { get; init; }
    public decimal? SecurityDeposit { get; init; }

    // Step 9 — Amenities (null = unchanged; empty list = clear all)
    public List<string>? Amenities { get; init; }
}
