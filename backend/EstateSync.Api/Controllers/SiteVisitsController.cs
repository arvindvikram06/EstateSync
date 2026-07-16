using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.SiteVisits;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/site-visits")]
[Authorize]
public class SiteVisitsController : ControllerBase
{
    private readonly ISiteVisitService _siteVisitService;

    public SiteVisitsController(ISiteVisitService siteVisitService)
    {
        _siteVisitService = siteVisitService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Tenant)]
    public async Task<ActionResult<ApiResponse<SiteVisitResponse>>> RequestVisit(CreateSiteVisitRequest request, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _siteVisitService.RequestVisitAsync(tenantId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.SiteVisitId },
            new ApiResponse<SiteVisitResponse> { Data = result, Message = "Site visit requested successfully." });
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<SiteVisitResponse>>> UpdateStatus(Guid id, UpdateSiteVisitStatusRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _siteVisitService.UpdateStatusAsync(userId, id, request, ct);
        return Ok(new ApiResponse<SiteVisitResponse> { Data = result, Message = "Site visit status updated." });
    }

    [HttpGet("tenant")]
    [Authorize(Roles = Roles.Tenant)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SiteVisitResponse>>>> GetTenantVisits(CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _siteVisitService.GetTenantVisitsAsync(tenantId, ct);
        return Ok(new ApiResponse<IEnumerable<SiteVisitResponse>> { Data = result });
    }

    [HttpGet("owner")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SiteVisitResponse>>>> GetOwnerVisits(CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        if (ownerId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _siteVisitService.GetOwnerVisitsAsync(ownerId, ct);
        return Ok(new ApiResponse<IEnumerable<SiteVisitResponse>> { Data = result });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SiteVisitResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _siteVisitService.GetByIdAsync(id, userId, ct);
        return Ok(new ApiResponse<SiteVisitResponse> { Data = result });
    }
}
