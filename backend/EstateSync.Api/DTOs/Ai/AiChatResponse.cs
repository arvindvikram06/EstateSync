using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Ai;

public sealed record AiChatSource(
    string Title,
    string ChunkId,
    string DocumentVersion);

public sealed record AiNavigationAction(
    string Label,
    string Route);

public sealed record AiChatResponse(
    string Reply,
    IReadOnlyList<AiChatSource> Sources,
    IReadOnlyList<AiNavigationAction> Actions);
