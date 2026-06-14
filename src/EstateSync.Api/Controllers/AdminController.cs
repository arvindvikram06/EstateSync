using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.DTOs.OwnerVerification;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.IdentityVerification;

using EstateSync.Api.Extensions;
using EstateSync.Api.Models;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IOwnerVerificationService _verificationService;
    private readonly IPropertyApprovalService _propertyApprovalService;
    private readonly IIdentityVerificationService _identityService;

    public AdminController(
        IOwnerVerificationService verificationService,
        IPropertyApprovalService propertyApprovalService,
        IIdentityVerificationService identityService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _propertyApprovalService = propertyApprovalService ?? throw new ArgumentNullException(nameof(propertyApprovalService));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    [HttpGet("owner-verification-requests")]
    public async Task<ActionResult<PagedResponse<OwnerVerificationResponse>>> GetOwnerVerificationRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _verificationService.GetPagedOwnerVerificationRequestsAsync(status, page, limit, ct);
        return Ok(result);
    }

    [HttpGet("properties/pending-approval")]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> GetPendingProperties(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _propertyApprovalService.GetPendingApprovalPropertiesAsync(page, limit, ct);
        return Ok(result);
    }

    [HttpPut("owner-verification-requests/{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyRequest(
        Guid id,
        [FromBody] VerifyOwnerRequest request,
        CancellationToken ct)
    {
        var adminId = User.GetUserId();
        await _verificationService.VerifyRequestAsync(id, adminId, request.Status.ToString(), request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Verification request status updated successfully." });
    }

    [HttpGet("identity-verification-requests")]
    public async Task<ActionResult<PagedResponse<IdentityVerificationResponse>>> GetIdentityVerificationRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _identityService.GetPagedIdentityRequestsAsync(status, page, limit, ct);
        return Ok(result);
    }

    [HttpPut("identity-verification-requests/{id:guid}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyIdentityRequest(
        Guid id,
        [FromBody] VerifyIdentityRequestDto request,
        CancellationToken ct)
    {
        var adminId = User.GetUserId();
        await _identityService.VerifyIdentityRequestAsync(id, adminId, request.Status.ToString(), request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Identity verification request status updated successfully." });
    }

    [HttpPut("properties/{id:guid}/approval")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveOrRejectProperty(
        Guid id,
        [FromBody] ApprovePropertyRequest request,
        CancellationToken ct)
    {
        var adminId = User.GetUserId();
        await _propertyApprovalService.ApproveOrRejectAsync(id, adminId, request.Status.ToString(), request.ApproveRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Property status updated successfully." });
    }


}

public record VerifyOwnerRequest(ApprovalStatus Status, string ReviewRemarks);
public record VerifyIdentityRequestDto(ApprovalStatus Status, string ReviewRemarks);
public record ApprovePropertyRequest(PropertyStatus Status, string ApproveRemarks);
