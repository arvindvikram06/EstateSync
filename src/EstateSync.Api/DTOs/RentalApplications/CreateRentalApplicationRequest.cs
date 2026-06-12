using System;

namespace EstateSync.Api.DTOs.RentalApplications;

public record CreateRentalApplicationRequest
{
    public required string Message { get; init; }
    public DateOnly ExpectedMoveInDate { get; init; }
    public int DurationMonths { get; init; }
    public decimal CommuteDistance { get; init; }
}
