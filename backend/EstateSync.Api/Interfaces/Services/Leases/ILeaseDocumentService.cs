using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Leases;

public interface ILeaseDocumentService
{
    /// <summary>
    /// Generates an unsigned agreement PDF from lease terms and stores it.
    /// Returns the stored file URL.
    /// </summary>
    Task<string> GenerateAgreementPdfAsync(Lease lease, CancellationToken ct = default);

    /// <summary>
    /// Generates the final signed agreement PDF embedding both parties' drawn signatures,
    /// typed names, timestamps, IPs, and the verification hash.
    /// Returns the stored file URL.
    /// </summary>
    Task<string> GenerateSignedAgreementPdfAsync(Lease lease, CancellationToken ct = default);
}
