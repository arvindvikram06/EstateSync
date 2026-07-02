using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Interfaces.Services.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardStatsResponse> GetStatsAsync(CancellationToken ct = default);
}
