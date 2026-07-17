using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Ai;
using EstateSync.Api.Interfaces.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
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

        var reply = await _aiService.ChatAsync(message, ct);
        return Ok(ApiResponse<string>.Ok(reply));
    }
}
