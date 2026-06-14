using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Complaints;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly ICurrentUserService _currentUserService;

    public ComplaintsController(IComplaintService complaintService, ICurrentUserService currentUserService)
    {
        _complaintService = complaintService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ComplaintResponse>> CreateComplaint(CreateComplaintRequest request, CancellationToken ct)
    {
        var tenantId = _currentUserService.UserId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.CreateComplaintAsync(tenantId.Value, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ComplaintId }, result);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ComplaintResponse>> UpdateStatus(Guid id, UpdateComplaintStatusRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UpdateComplaintStatusAsync(userId.Value, id, request, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComplaintResponse>> GetById(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetByIdAsync(id, userId.Value, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComplaintResponse>>> GetMyComplaints(CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.GetMyComplaintsAsync(userId.Value, ct);
        return Ok(result);
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ComplaintResponse>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        var tenantId = _currentUserService.UserId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty) throw new UnauthorizedAccessException();

        var result = await _complaintService.UploadComplaintImageAsync(tenantId.Value, id, file, ct);
        return Ok(result);
    }
}
