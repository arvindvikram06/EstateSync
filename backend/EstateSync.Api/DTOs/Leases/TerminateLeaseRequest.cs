namespace EstateSync.Api.DTOs.Leases;

public class TerminateLeaseRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool ForfeitDeposit { get; set; } = true;
}
