using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Interfaces.Services.Complaints;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintsController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpPost]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<ComplaintResponse>>> CreateComplaint(CreateComplaintRequest request, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.CreateComplaintAsync(tenantId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ComplaintId },
            new ApiResponse<ComplaintResponse> { Data = result, Message = "Complaint created successfully." });
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<ComplaintResponse>>> UpdateStatus(Guid id, UpdateComplaintStatusRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UpdateComplaintStatusAsync(userId, id, request, ct);
        return Ok(new ApiResponse<ComplaintResponse> { Data = result, Message = "Complaint status updated." });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ComplaintResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetByIdAsync(id, userId, ct);
        return Ok(new ApiResponse<ComplaintResponse> { Data = result });
    }

    // Explicit per-view routes: a user holding both roles sees only the
    // complaints they RAISED on the tenant dashboard and only the complaints
    // AGAINST their properties on the owner dashboard.
    [HttpGet("tenant")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ComplaintResponse>>>> GetTenantComplaints(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetTenantComplaintsAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<ComplaintResponse>> { Data = result });
    }

    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ComplaintResponse>>>> GetOwnerComplaints(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetOwnerComplaintsAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<ComplaintResponse>> { Data = result });
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<ComplaintResponse>>> UploadImage(Guid id, [FromForm] IFormFile file, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UploadComplaintImageAsync(tenantId, id, file, ct);
        return Ok(new ApiResponse<ComplaintResponse> { Data = result });
    }

    [HttpPost("{id:guid}/owner-viewed")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<ActionResult<ApiResponse<object>>> MarkOwnerViewed(Guid id, CancellationToken ct)
    {
        await _complaintService.MarkOwnerViewedAsync(User.GetUserId(), id, ct);
        return Ok(new ApiResponse<object> { Message = "Complaint acknowledged by owner." });
    }
}
