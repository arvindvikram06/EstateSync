using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Ai;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiService aiService, ILogger<AiController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    [HttpPost("chat")]
    [EnableRateLimiting("ai-chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponse>>> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        var message = new string(request.Message
            .Where(c => !char.IsControl(c))
            .ToArray())
            .Trim();

        if (string.IsNullOrEmpty(message))
            return BadRequest(ApiResponse<AiChatResponse>.Fail("Message cannot be empty."));

        if (message.Length > 500)
            return BadRequest(ApiResponse<AiChatResponse>.Fail("Message must be 500 characters or fewer."));

        var userId = User.GetUserId();
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var history = request.History
            .Where(turn =>
                (turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                 turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrWhiteSpace(turn.Content))
            .TakeLast(10)
            .Select(turn => new AiChatHistoryTurn
            {
                Role = turn.Role.ToLowerInvariant(),
                Content = new string(turn.Content
                    .Where(c => !char.IsControl(c))
                    .Take(1000)
                    .ToArray())
                    .Trim()
            })
            .Where(turn => turn.Content.Length > 0)
            .ToArray();
        _logger.LogInformation(
            "AI chat request from user {UserId} ({MessageLength} characters, {HistoryCount} prior turns)",
            userId,
            message.Length,
            history.Length);

        var reply = await _aiService.ChatAsync(message, roles, history, ct);
        return Ok(ApiResponse<AiChatResponse>.Ok(reply));
    }
}
