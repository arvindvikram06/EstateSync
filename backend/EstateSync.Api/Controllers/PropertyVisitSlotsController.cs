using EstateSync.Api.Common;
using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.SiteVisits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/properties/{propertyId:guid}/visit-slots")]
[Authorize]
public sealed class PropertyVisitSlotsController : ControllerBase
{
    private readonly IPropertyVisitSlotService _service;
    public PropertyVisitSlotsController(IPropertyVisitSlotService service) => _service = service;

    [HttpPost("bulk")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>>>> Create(
        Guid propertyId, CreateVisitSlotsRequest request, CancellationToken ct)
    {
        var data = await _service.CreateAsync(User.GetUserId(), propertyId, request, ct);
        return Ok(new ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>> { Data = data });
    }

    [HttpGet("manage")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>>>> Manage(
        Guid propertyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var data = await _service.GetManageAsync(User.GetUserId(), propertyId, from, to, ct);
        return Ok(new ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>> { Data = data });
    }

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>>>> Available(
        Guid propertyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var data = await _service.GetAvailableAsync(propertyId, from, to, ct);
        return Ok(new ApiResponse<IReadOnlyCollection<PropertyVisitSlotResponse>> { Data = data });
    }

    [HttpPut("{slotId:guid}/block")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<PropertyVisitSlotResponse>>> Block(
        Guid propertyId, Guid slotId, CancellationToken ct)
    {
        var data = await _service.SetBlockedAsync(User.GetUserId(), propertyId, slotId, true, ct);
        return Ok(new ApiResponse<PropertyVisitSlotResponse> { Data = data });
    }

    [HttpPut("{slotId:guid}/unblock")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<PropertyVisitSlotResponse>>> Unblock(
        Guid propertyId, Guid slotId, CancellationToken ct)
    {
        var data = await _service.SetBlockedAsync(User.GetUserId(), propertyId, slotId, false, ct);
        return Ok(new ApiResponse<PropertyVisitSlotResponse> { Data = data });
    }

    [HttpDelete("{slotId:guid}")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<IActionResult> Delete(Guid propertyId, Guid slotId, CancellationToken ct)
    {
        await _service.DeleteAsync(User.GetUserId(), propertyId, slotId, ct);
        return NoContent();
    }
}
