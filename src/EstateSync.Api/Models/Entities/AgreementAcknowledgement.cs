using System;

namespace EstateSync.Api.Models;

public class AgreementAcknowledgement
{
    public Guid AcknowledgementId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public bool Acknowledge { get; set; }
    public DateTime CreatedAt { get; set; }
}
