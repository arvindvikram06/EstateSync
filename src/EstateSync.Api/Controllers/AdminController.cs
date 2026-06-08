using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Properties;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IOwnerVerificationService _verificationService;
    private readonly IPropertyApprovalService _propertyApprovalService;

    public AdminController(
        IOwnerVerificationService verificationService,
        IPropertyApprovalService propertyApprovalService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _propertyApprovalService = propertyApprovalService ?? throw new ArgumentNullException(nameof(propertyApprovalService));
    }

    [HttpGet("owner-verification-requests")]
    public async Task<ActionResult<ApiResponse<PagedResponse<VerificationRequestResponse>>>> GetOwnerVerificationRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _verificationService.GetAdminVerificationRequestsAsync(status, page, limit, ct);
        return Ok(new ApiResponse<PagedResponse<VerificationRequestResponse>> { Data = result });
    }

    [HttpGet("properties/pending-approval")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PropertySummaryResponse>>>> GetPendingProperties(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _propertyApprovalService.GetPendingApprovalPropertiesAsync(page, limit, ct);
        return Ok(new ApiResponse<PagedResponse<PropertySummaryResponse>> { Data = result });
    }

    [HttpPut("owner-verification-requests/{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyRequest(
        Guid id,
        [FromBody] VerifyOwnerRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        await _verificationService.VerifyRequestAsync(id, adminId, request.Status, request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Verification request status updated successfully." });
    }

    [HttpPut("properties/{id:guid}/approval")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveOrRejectProperty(
        Guid id,
        [FromBody] ApprovePropertyRequest request,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        await _propertyApprovalService.ApproveOrRejectAsync(id, adminId, request.Status, request.ApproveRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Property status updated successfully." });
    }


    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }
}

public record VerifyOwnerRequest(string Status, string ReviewRemarks);
public record ApprovePropertyRequest(string Status, string ApproveRemarks);
