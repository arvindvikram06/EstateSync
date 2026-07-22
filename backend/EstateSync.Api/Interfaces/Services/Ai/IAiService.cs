using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EstateSync.Api.DTOs.Ai;

namespace EstateSync.Api.Interfaces.Services.Ai;

public interface IAiService
{
    Task<AiChatResponse> ChatAsync(
        string message,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<AiChatHistoryTurn> history,
        CancellationToken ct = default);
}
