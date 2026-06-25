using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Interfaces.Services.Leases;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ILeaseService _leaseService;

    public LeasesController(ILeaseService leaseService)
    {
        _leaseService = leaseService ?? throw new ArgumentNullException(nameof(leaseService));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<LeaseResponse>>> Create(
        [FromBody] CreateLeaseRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _leaseService.CreateLeaseAsync(request, ownerId, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<LeaseResponse>
        {
            Data = result,
            Message = "Lease agreement initiated successfully."
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<LeaseResponse>>> Update(
        Guid id,
        [FromBody] UpdateLeaseRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _leaseService.UpdateLeaseAsync(id, request, ownerId, ct);
        return Ok(new ApiResponse<LeaseResponse>
        {
            Data = result,
            Message = "Lease updated successfully."
        });
    }

    [HttpPut("{id:guid}/sign")]
    [Authorize(Roles = "Owner,Tenant")]
    public async Task<ActionResult<ApiResponse<LeaseResponse>>> Sign(
        Guid id,
        [FromBody] SignLeaseRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _leaseService.SignLeaseAsync(id, request, userId, ct);

        return Ok(new ApiResponse<LeaseResponse>
        {
            Data = result,
            Message = "Lease agreement signed successfully."
        });
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> VoidLease(
        Guid id,
        [FromBody] VoidLeaseRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _leaseService.VoidLeaseAsync(id, request.Reason, ownerId, ct);
        return Ok(new ApiResponse<object> { Message = "Lease voided successfully." });
    }


    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Owner,Tenant,Admin")]
    public async Task<ActionResult<ApiResponse<LeaseResponse>>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _leaseService.GetLeaseByIdAsync(id, userId, isAdmin, ct);

        return Ok(new ApiResponse<LeaseResponse>
        {
            Data = result
        });
    }

    [HttpGet("tenant")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<PagedResponse<LeaseResponse>>> GetTenantLeases(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var tenantId = User.GetUserId();
        var result = await _leaseService.GetTenantLeasesAsync(tenantId, page, limit, ct);
        return Ok(result);
    }

    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<PagedResponse<LeaseResponse>>> GetOwnerLeases(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var ownerId = User.GetUserId();
        var result = await _leaseService.GetOwnerLeasesAsync(ownerId, page, limit, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<LeaseResponse>>> GetAllLeases(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _leaseService.GetAllLeasesAsync(page, limit, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Terminate(
        Guid id,
        [FromBody] TerminateLeaseRequest request,
        CancellationToken ct)
    {
        var requesterId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        await _leaseService.TerminateLeaseAsync(id, requesterId, isAdmin, request.Reason, ct);
        return Ok(new ApiResponse<object> { Message = "Lease terminated successfully." });
    }

    [HttpPost("{id:guid}/request-renewal")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> RequestRenewal(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        await _leaseService.RequestRenewalAsync(id, tenantId, ct);
        return Ok(new ApiResponse<object> { Message = "Renewal requested successfully." });
    }

    [HttpPost("{id:guid}/reject-renewal")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> RejectRenewal(Guid id, CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _leaseService.RejectRenewalAsync(id, ownerId, ct);
        return Ok(new ApiResponse<object> { Message = "Renewal rejected successfully." });
    }
}
