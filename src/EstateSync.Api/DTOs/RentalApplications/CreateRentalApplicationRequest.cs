using System;

namespace EstateSync.Api.DTOs.RentalApplications;

public record CreateRentalApplicationRequest
{
    public required string Message { get; init; }
    public DateTime MoveInDate { get; init; }
    public DateTime MoveOutDate { get; init; }
    public decimal NegotiatedPrice { get; init; }
    public decimal CommuteDistance { get; init; }
}
