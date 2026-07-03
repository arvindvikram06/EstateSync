using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.DTOs.Payments;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IStripeConnectedAccountService _connectedAccountService;
    private readonly ITenantPaymentService _tenantPaymentService;
    private readonly IStripeWebhookService _webhookService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeConnectedAccountService connectedAccountService,
        ITenantPaymentService tenantPaymentService,
        IStripeWebhookService webhookService,
        IConfiguration config,
        ILogger<PaymentsController> logger)
    {
        _connectedAccountService = connectedAccountService;
        _tenantPaymentService    = tenantPaymentService;
        _webhookService          = webhookService;
        _config                  = config;
        _logger                  = logger;
    }

    // ── Owner: Stripe Connect onboarding ─────────────────────────

    [HttpPost("owner-onboard")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> OnboardOwner(CancellationToken ct)
    {
        var url = await _connectedAccountService.CreateOwnerOnboardingLinkAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = new { url } });
    }

    [HttpPost("owner/stripe-login-link")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> GetStripeLoginLink(CancellationToken ct)
    {
        var url = await _connectedAccountService.CreateStripeLoginLinkAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = new { url } });
    }

    [HttpGet("owner/settlements")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<PagedResponse<OwnerSettlementResponse>>> GetOwnerSettlements(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _connectedAccountService.GetOwnerSettlementsAsync(User.GetUserId(), status, page, limit, ct);
        return Ok(result);
    }

    [HttpGet("owner/earnings-summary")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> GetOwnerEarningsSummary(CancellationToken ct)
    {
        var summary = await _connectedAccountService.GetOwnerEarningsSummaryAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = summary });
    }

    [HttpGet("owner/available-balance")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<OwnerAvailableBalanceResponse>>> GetAvailableBalance(CancellationToken ct)
    {
        var result = await _connectedAccountService.GetAvailableBalanceAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<OwnerAvailableBalanceResponse> { Data = result });
    }

    [HttpPost("owner/request-payout")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<RequestPayoutResponse>>> RequestPayout(
        [FromBody] RequestPayoutRequest request, CancellationToken ct)
    {
        var result = await _connectedAccountService.RequestPayoutAsync(User.GetUserId(), request.Method, ct);
        return Ok(new ApiResponse<RequestPayoutResponse> { Data = result });
    }

    // ── Tenant: bills and checkout ────────────────────────────────

    [HttpPost("checkout/{leaseBillId:guid}")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> CreateCheckout(Guid leaseBillId, CancellationToken ct)
    {
        var url = await _tenantPaymentService.CreateTenantCheckoutSessionAsync(leaseBillId, User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = new { url } });
    }

    [HttpGet("tenant/bills")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> GetTenantBills(CancellationToken ct)
    {
        var bills = await _tenantPaymentService.GetTenantBillsAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = bills });
    }

    // ── Stripe webhooks ───────────────────────────────────────────

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _webhookService.HandleAsync(json, signature, ct);
        return Ok();
    }

    // ── Stripe Connect redirect pages (Stripe → backend → frontend) ──────────
    // These are the ReturnUrl / RefreshUrl that Stripe calls after Express onboarding.
    // They redirect to the configured Angular frontend routes so no URLs are hardcoded here.

    [HttpGet("success")]
    [AllowAnonymous]
    public ActionResult Success()
    {
        var returnUrl = _config["Stripe:OnboardingReturnUrl"]
            ?? throw new InvalidOperationException("Stripe:OnboardingReturnUrl is not configured.");
        return Redirect(returnUrl);
    }

    [HttpGet("refresh")]
    [AllowAnonymous]
    public ActionResult Refresh()
    {
        var refreshUrl = _config["Stripe:OnboardingRefreshUrl"]
            ?? throw new InvalidOperationException("Stripe:OnboardingRefreshUrl is not configured.");
        return Redirect(refreshUrl);
    }

    // NOTE: CheckoutSuccessUrl and CheckoutCancelUrl in appsettings already point directly to
    // the Angular frontend routes, so Stripe redirects tenants there without hitting the backend.
    // These endpoints are kept as a fallback in case config is ever changed to point here.
}
