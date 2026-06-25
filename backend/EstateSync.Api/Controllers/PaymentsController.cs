using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("owner-onboard")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> OnboardOwner(CancellationToken ct)
    {
        var url = await _paymentService.CreateOwnerOnboardingLinkAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = new { url } });
    }

    [HttpPost("checkout/{leaseBillId:guid}")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> CreateCheckout(Guid leaseBillId, CancellationToken ct)
    {
        var url = await _paymentService.CreateTenantCheckoutSessionAsync(leaseBillId, User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = new { url } });
    }

    [HttpGet("tenant/bills")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> GetTenantBills(CancellationToken ct)
    {
        var bills = await _paymentService.GetTenantBillsAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<object> { Data = bills });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _paymentService.HandleWebhookAsync(json, signature, ct);
        return Ok();
    }

    [HttpGet("success")]
    [AllowAnonymous]
    public ActionResult Success() => Ok("Payment Successful / Onboarding Complete!");

    [HttpGet("refresh")]
    [AllowAnonymous]
    public ActionResult Refresh() => Ok("Session expired. Please request a new link.");

    [HttpGet("checkout-success")]
    [AllowAnonymous]
    public ActionResult CheckoutSuccess() => Ok("Checkout Successful!");

    [HttpGet("checkout-cancel")]
    [AllowAnonymous]
    public ActionResult CheckoutCancel() => Ok("Checkout Cancelled!");
}
