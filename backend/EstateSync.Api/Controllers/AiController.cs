using System.Threading;
using System.Threading.Tasks;
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
    public async Task<ActionResult<ApiResponse<string>>> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        var message = new string(request.Message
            .Where(c => !char.IsControl(c))
            .ToArray())
            .Trim();

        if (string.IsNullOrEmpty(message))
            return BadRequest(ApiResponse<string>.Fail("Message cannot be empty."));

        if (message.Length > 500)
            return BadRequest(ApiResponse<string>.Fail("Message must be 500 characters or fewer."));

        var userId = User.GetUserId();
        _logger.LogInformation("AI chat request from user {UserId}: {Message}", userId, message);

        var reply = await _aiService.ChatAsync(message, ct);
        return Ok(ApiResponse<string>.Ok(reply));
    }
}
