using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Ai;
using EstateSync.Api.DTOs.Ai;
using Microsoft.Extensions.Configuration;

namespace EstateSync.Api.Services.Ai;

public class AiService : IAiService
{
    private readonly HttpClient _http;

    public AiService(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _http.BaseAddress = new System.Uri(config["AiService:BaseUrl"] ?? "http://localhost:8000");
    }

    public async Task<AiChatResponse> ChatAsync(
        string message,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<AiChatHistoryTurn> history,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/chat", new { message, roles, history }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AiChatResponse>(cancellationToken: ct);
        return result ?? throw new JsonException("AI service returned an empty response.");
    }
}
