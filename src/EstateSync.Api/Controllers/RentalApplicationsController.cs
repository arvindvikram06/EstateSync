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
        var tenantId = GetUserId();
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
        var ownerId = GetUserId();
        var response = await _rentalAppService.ReviewApplicationAsync(applicationId, request.Approve, request.Remarks, ownerId, ct);

        return Ok(new ApiResponse<RentalApplicationResponse>
        {
            Data = response,
            Message = request.Approve ? "Rental application approved successfully." : "Rental application rejected successfully."
        });
    }

    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RentalApplicationResponse>>>> GetAllApplications(CancellationToken ct)
    {
        var ownerId = GetUserId();
        var response = await _rentalAppService.GetOwnerApplicationsAsync(ownerId, ct);

        return Ok(new ApiResponse<IEnumerable<RentalApplicationResponse>>
        {
            Data = response
        });
    }

    [HttpGet("properties/{propertyId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RentalApplicationResponse>>>> GetPropertyApplications(
        Guid propertyId,
        CancellationToken ct)
    {
        var ownerId = GetUserId();
        var response = await _rentalAppService.GetApplicationsByPropertyIdAsync(propertyId, ownerId, ct);

        return Ok(new ApiResponse<IEnumerable<RentalApplicationResponse>>
        {
            Data = response
        });
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }
}
