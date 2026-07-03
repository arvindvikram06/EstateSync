using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Admin;
using EstateSync.Api.Interfaces.Services.Complaints;
using EstateSync.Api.Interfaces.Services.Kyc;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.TenantFinancialProfile;
using EstateSync.Api.Models;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IPropertyApprovalService _propertyApprovalService;
    private readonly IKycService _kycService;
    private readonly ITenantFinancialProfileService _financialProfileService;
    private readonly IAdminService _adminService;
    private readonly IAdminDashboardService _dashboardService;
    private readonly IAdminFinancialService _financialService;
    private readonly IComplaintService _complaintService;

    public AdminController(
        IPropertyApprovalService propertyApprovalService,
        IKycService kycService,
        ITenantFinancialProfileService financialProfileService,
        IAdminService adminService,
        IAdminDashboardService dashboardService,
        IAdminFinancialService financialService,
        IComplaintService complaintService)
    {
        _propertyApprovalService = propertyApprovalService ?? throw new ArgumentNullException(nameof(propertyApprovalService));
        _kycService              = kycService              ?? throw new ArgumentNullException(nameof(kycService));
        _financialProfileService = financialProfileService ?? throw new ArgumentNullException(nameof(financialProfileService));
        _adminService            = adminService            ?? throw new ArgumentNullException(nameof(adminService));
        _dashboardService        = dashboardService        ?? throw new ArgumentNullException(nameof(dashboardService));
        _financialService        = financialService        ?? throw new ArgumentNullException(nameof(financialService));
        _complaintService        = complaintService        ?? throw new ArgumentNullException(nameof(complaintService));
    }

    // ── Dashboard Stats ───────────────────────────────────────────────────────

    [HttpGet("dashboard/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStatsResponse>>> GetDashboardStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(ct);
        return Ok(new ApiResponse<AdminDashboardStatsResponse> { Data = stats });
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

    [HttpGet("kyc-requests/{id:guid}")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<KycVerificationResponse>>> GetKycRequestById(Guid id, CancellationToken ct)
    {
        var result = await _kycService.GetByIdForAdminAsync(id, ct);
        return Ok(new ApiResponse<KycVerificationResponse> { Data = result });
    }

    [HttpPut("kyc-requests/{id:guid}/review")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewKycRequest(
        Guid id, [FromBody] ReviewVerificationRequest request, CancellationToken ct)
    {
        await _kycService.ReviewAsync(id, User.GetUserId(), request.Status.ToString(), request.ReviewRemarks, ct);
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

    [HttpGet("financial-profiles/{id:guid}")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<TenantFinancialProfileResponse>>> GetFinancialProfileById(Guid id, CancellationToken ct)
    {
        var result = await _financialProfileService.GetByIdForAdminAsync(id, ct);
        return Ok(new ApiResponse<TenantFinancialProfileResponse> { Data = result });
    }

    [HttpPut("financial-profiles/{id:guid}/review")]
    [Authorize(Roles = "Admin,Verifier")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewFinancialProfile(
        Guid id, [FromBody] ReviewVerificationRequest request, CancellationToken ct)
    {
        await _financialProfileService.ReviewAsync(id, User.GetUserId(), request.Status.ToString(), request.ReviewRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Financial profile reviewed successfully." });
    }

    // ── Properties ────────────────────────────────────────────────────────────

    [HttpGet("properties/pending-approval")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> GetPendingProperties(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _propertyApprovalService.GetPendingApprovalPropertiesAsync(page, limit, ct);
        return Ok(result);
    }

    [HttpPut("properties/{id:guid}/approval")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveOrRejectProperty(
        Guid id, [FromBody] ApprovePropertyRequest request, CancellationToken ct)
    {
        await _propertyApprovalService.ApproveOrRejectAsync(id, User.GetUserId(), request.Status.ToString(), request.ApproveRemarks, ct);
        return Ok(new ApiResponse<object> { Message = "Property status updated successfully." });
    }

    // ── Lease Approval ────────────────────────────────────────────────────────

    [HttpPut("leases/{leaseId:guid}/approval")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveLease(
        Guid leaseId, [FromBody] AdminLeaseApprovalRequest request, CancellationToken ct)
    {
        await _adminService.ApproveLeaseAsync(leaseId, request.Approve, request.Remarks, User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Message = request.Approve ? "Lease approved and activated." : "Lease rejected." });
    }

    // ── Staff / Verifier Management ───────────────────────────────────────────

    [HttpGet("staff")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> GetStaff(CancellationToken ct)
    {
        var staff = await _adminService.GetStaffAsync(ct);
        return Ok(new ApiResponse<object> { Data = staff });
    }

    [HttpPost("staff")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> CreateVerifier(
        [FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        await _adminService.CreateVerifierAsync(request, ct);
        return StatusCode(201, new ApiResponse<object> { Message = $"Verifier account created for {request.Email}." });
    }

    [HttpPut("staff/{userId:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateStaff(Guid userId, CancellationToken ct)
    {
        await _adminService.DeactivateStaffAsync(userId, ct);
        return Ok(new ApiResponse<object> { Message = "Verifier account deactivated." });
    }

    // ── Complaints ────────────────────────────────────────────────────────────

    [HttpGet("complaints")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<ComplaintResponse>>> GetComplaints(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _complaintService.GetPagedForAdminAsync(status, priority, page, limit, ct);
        return Ok(result);
    }

    // ── Financial Dashboard ───────────────────────────────────────────────────

    [HttpGet("financial/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SettlementSummaryResponse>>> GetFinancialSummary(CancellationToken ct)
    {
        var summary = await _financialService.GetFinancialSummaryAsync(ct);
        return Ok(new ApiResponse<SettlementSummaryResponse> { Data = summary });
    }

    [HttpGet("settlements")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<OwnerSettlementResponse>>> GetSettlements(
        [FromQuery] Guid? ownerId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _financialService.GetSettlementsAsync(ownerId, status, from, to, page, limit, ct);
        return Ok(result);
    }

    [HttpGet("connected-accounts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<ConnectedAccountResponse>>> GetConnectedAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _financialService.GetConnectedAccountsAsync(page, limit, ct);
        return Ok(result);
    }

    [HttpPost("connected-accounts/{id}/sync")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ConnectedAccountResponse>>> SyncConnectedAccount(
        Guid id, CancellationToken ct)
    {
        var result = await _financialService.SyncConnectedAccountAsync(id, ct);
        return Ok(ApiResponse<ConnectedAccountResponse>.Ok(result, "Account synced from Stripe."));
    }

    [HttpGet("payments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<AdminPaymentResponse>>> GetPayments(
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _financialService.GetPaymentsAsync(status, from, to, page, limit, ct);
        return Ok(result);
    }
}

public record ReviewVerificationRequest(ApprovalStatus Status, string ReviewRemarks);
public record ApprovePropertyRequest(PropertyStatus Status, string ApproveRemarks);
