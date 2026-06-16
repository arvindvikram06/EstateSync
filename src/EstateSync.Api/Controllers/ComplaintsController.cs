using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<ComplaintResponse>> CreateComplaint(CreateComplaintRequest request, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.CreateComplaintAsync(tenantId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ComplaintId }, result);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ComplaintResponse>> UpdateStatus(Guid id, UpdateComplaintStatusRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UpdateComplaintStatusAsync(userId, id, request, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComplaintResponse>> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetByIdAsync(id, userId, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComplaintResponse>>> GetMyComplaints(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetMyComplaintsAsync(userId, ct);
        return Ok(result);
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ComplaintResponse>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UploadComplaintImageAsync(tenantId, id, file, ct);
        return Ok(result);
    }
}
