using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Ai;

public sealed record AiChatHistoryTurn
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public record AiChatRequest
{
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<AiChatHistoryTurn> History { get; init; } = [];
}
