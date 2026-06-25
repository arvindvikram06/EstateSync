using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Admin;
using EstateSync.Api.Interfaces.Services.Kyc;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.TenantFinancialProfile;
using EstateSync.Api.Models;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IPropertyApprovalService _propertyApprovalService;
    private readonly IKycService _kycService;
    private readonly ITenantFinancialProfileService _financialProfileService;
    private readonly IAdminService _adminService;

    public AdminController(
        IPropertyApprovalService propertyApprovalService,
        IKycService kycService,
        ITenantFinancialProfileService financialProfileService,
        IAdminService adminService)
    {
        _propertyApprovalService = propertyApprovalService ?? throw new ArgumentNullException(nameof(propertyApprovalService));
        _kycService = kycService ?? throw new ArgumentNullException(nameof(kycService));
        _financialProfileService = financialProfileService ?? throw new ArgumentNullException(nameof(financialProfileService));
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    }

    // ── KYC — Admin and Verifier ──────────────────────────────────────────────

    [HttpGet("kyc-requests")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<PagedResponse<KycVerificationResponse>>> GetKycRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _kycService.GetPagedAsync(status, page, limit, ct);
        return Ok(result);
    }

    [HttpPut("kyc-requests/{id:guid}/review")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewKycRequest(
        Guid id,
        [FromBody] ReviewVerificationRequest request,
        CancellationToken ct)
    {
        var reviewerId = User.GetUserId();
        await _kycService.ReviewAsync(id, reviewerId, request.Status.ToString(), request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "KYC request reviewed successfully." });
    }

    // ── Tenant Financial Profile — Admin and Verifier ─────────────────────────

    [HttpGet("financial-profiles")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<PagedResponse<TenantFinancialProfileResponse>>> GetFinancialProfiles(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _financialProfileService.GetPagedAsync(status, page, limit, ct);
        return Ok(result);
    }

    [HttpPut("financial-profiles/{id:guid}/review")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewFinancialProfile(
        Guid id,
        [FromBody] ReviewVerificationRequest request,
        CancellationToken ct)
    {
        var reviewerId = User.GetUserId();
        await _financialProfileService.ReviewAsync(id, reviewerId, request.Status.ToString(), request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Financial profile reviewed successfully." });
    }

    // ── Properties — Admin only ───────────────────────────────────────────────

    [HttpGet("properties/pending-approval")]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> GetPendingProperties(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _propertyApprovalService.GetPendingApprovalPropertiesAsync(page, limit, ct);
        return Ok(result);
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

    // ── Lease Approval — Admin only ───────────────────────────────────────────

    [HttpPut("leases/{leaseId:guid}/approval")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveLease(
        Guid leaseId,
        [FromBody] AdminLeaseApprovalRequest request,
        CancellationToken ct)
    {
        var adminId = User.GetUserId();
        await _adminService.ApproveLeaseAsync(leaseId, request.Approve, request.Remarks, adminId, ct);
        var msg = request.Approve ? "Lease approved and activated." : "Lease rejected.";
        return Ok(new ApiResponse<object> { Message = msg });
    }

    // ── Staff Management — Admin only ─────────────────────────────────────────

    [HttpPost("staff")]
    public async Task<ActionResult<ApiResponse<object>>> CreateVerifier(
        [FromBody] CreateStaffRequest request,
        CancellationToken ct)
    {
        await _adminService.CreateVerifierAsync(request, ct);
        return StatusCode(201, new ApiResponse<object> { Message = $"Verifier account created for {request.Email}." });
    }

    [HttpPut("staff/{userId:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateStaff(Guid userId, CancellationToken ct)
    {
        await _adminService.DeactivateStaffAsync(userId, ct);
        return Ok(new ApiResponse<object> { Message = "Verifier account deactivated." });
    }
}

public record ReviewVerificationRequest(ApprovalStatus Status, string ReviewRemarks);
public record ApprovePropertyRequest(PropertyStatus Status, string ApproveRemarks);
