using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class KycDocument : BaseEntity
{
    public Guid KycVerificationId { get; set; }
    public KycVerification? KycVerification { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
}
