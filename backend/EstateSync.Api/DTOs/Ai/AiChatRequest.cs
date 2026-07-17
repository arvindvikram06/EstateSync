namespace EstateSync.Api.DTOs.Ai;

public record AiChatRequest
{
    public string Message { get; init; } = string.Empty;
}
