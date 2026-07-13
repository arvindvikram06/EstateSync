using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SavedPropertiesController : ControllerBase
{
    private readonly ISavedPropertyService _savedPropertyService;

    public SavedPropertiesController(ISavedPropertyService savedPropertyService)
    {
        _savedPropertyService = savedPropertyService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> GetSaved(
        [FromQuery] int page = 1, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        var result = await _savedPropertyService.GetSavedAsync(User.GetUserId(), page, limit, ct);
        return Ok(result);
    }

    [HttpGet("ids")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GetSavedIds(CancellationToken ct)
    {
        var ids = await _savedPropertyService.GetSavedIdsAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<List<Guid>> { Data = ids });
    }

    [HttpPost("{propertyId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Save(Guid propertyId, CancellationToken ct)
    {
        await _savedPropertyService.SaveAsync(User.GetUserId(), propertyId, ct);
        return Ok(new ApiResponse<object> { Message = "Property saved." });
    }

    [HttpDelete("{propertyId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Unsave(Guid propertyId, CancellationToken ct)
    {
        await _savedPropertyService.UnsaveAsync(User.GetUserId(), propertyId, ct);
        return Ok(new ApiResponse<object> { Message = "Property removed from saved." });
    }
}
