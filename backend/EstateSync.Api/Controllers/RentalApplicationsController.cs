using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.RentalApplications;
using EstateSync.Api.Interfaces.Services.RentalApplications;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RentalApplicationsController : ControllerBase
{
    private readonly IRentalApplicationService _rentalAppService;

    public RentalApplicationsController(IRentalApplicationService rentalAppService)
    {
        _rentalAppService = rentalAppService ?? throw new ArgumentNullException(nameof(rentalAppService));
    }

    [HttpPost("properties/{propertyId:guid}")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<RentalApplicationResponse>>> CreateApplication(
        Guid propertyId,
        [FromBody] CreateRentalApplicationRequest request,
        CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var response = await _rentalAppService.CreateApplicationAsync(propertyId, request, tenantId, ct);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<RentalApplicationResponse>
        {
            Data = response,
            Message = "Rental application submitted successfully."
        });
    }

    [HttpPut("{applicationId:guid}/review")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<RentalApplicationResponse>>> ReviewApplication(
        Guid applicationId,
        [FromBody] ReviewRentalApplicationRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var response = await _rentalAppService.ReviewApplicationAsync(applicationId, request.Approve, request.Remarks, ownerId, ct);

        return Ok(new ApiResponse<RentalApplicationResponse>
        {
            Data = response,
            Message = request.Approve ? "Rental application approved successfully." : "Rental application rejected successfully."
        });
    }

    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<PagedResponse<RentalApplicationResponse>>> GetAllApplications(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var ownerId = User.GetUserId();
        var response = await _rentalAppService.GetOwnerApplicationsAsync(ownerId, page, limit, ct);

        return Ok(response);
    }

    [HttpGet("properties/{propertyId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<PagedResponse<RentalApplicationResponse>>> GetPropertyApplications(
        Guid propertyId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var ownerId = User.GetUserId();
        var response = await _rentalAppService.GetApplicationsByPropertyIdAsync(propertyId, ownerId, page, limit, ct);

        return Ok(response);
    }

    [HttpGet("tenant")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<PagedResponse<RentalApplicationResponse>>> GetTenantApplications(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var tenantId = User.GetUserId();
        var response = await _rentalAppService.GetTenantApplicationsAsync(tenantId, page, limit, ct);

        return Ok(response);
    }

    [HttpGet("{applicationId:guid}")]
    [Authorize(Roles = "Tenant,Owner,Admin")]
    public async Task<ActionResult<ApiResponse<RentalApplicationResponse>>> GetApplicationById(
        Guid applicationId,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var response = await _rentalAppService.GetApplicationByIdAsync(applicationId, userId, isAdmin, ct);

        return Ok(new ApiResponse<RentalApplicationResponse>
        {
            Data = response
        });
    }

    [HttpDelete("{applicationId:guid}")]
    [Authorize(Roles = "Tenant")]
    public async Task<IActionResult> DeleteApplication(Guid applicationId, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        await _rentalAppService.DeleteApplicationAsync(applicationId, tenantId, ct);
        return NoContent();
    }

}

