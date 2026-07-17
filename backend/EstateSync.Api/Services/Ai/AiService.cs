using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Ai;
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

    public async Task<string> ChatAsync(string message, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/chat", new { message }, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("reply").GetString() ?? string.Empty;
    }
}
