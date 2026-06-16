using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using EstateSync.Api.Extensions;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;
using System.Collections.Generic;
namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("owner-onboard")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> OnboardOwner(CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        if (ownerId == Guid.Empty) throw new UnauthorizedAccessException();

        var url = await _paymentService.CreateOwnerOnboardingLinkAsync(ownerId, ct);
        return Ok(new { url });
    }

    [HttpPost("checkout/{leaseBillId}")]
    [Authorize(Roles = "Tenant")]
    public async Task<IActionResult> CreateCheckout(Guid leaseBillId, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var url = await _paymentService.CreateTenantCheckoutSessionAsync(leaseBillId, tenantId, ct);
        return Ok(new { url });
    }

    [HttpGet("tenant/bills")]
    [Authorize(Roles = "Tenant")]
    public async Task<IActionResult> GetTenantBills(
        [FromServices] ILeaseBillRepository leaseBillRepo,
        CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        if (tenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var bills = await leaseBillRepo.GetByTenantIdAsync(tenantId);
        var response = bills.Select(b => new LeaseBillResponse
        {
            LeaseBillId = b.LeaseBillId,
            LeaseId = b.LeaseId,
            PropertyTitle = b.Lease?.Property?.Title ?? "Unknown Property",
            BillType = b.BillType,
            DueDate = b.DueDate,
            Amount = b.Amount,
            PendingAmount = b.PendingAmount
        });

        return Ok(new ApiResponse<IEnumerable<LeaseBillResponse>> 
        { 
            Data = response 
        });
    }

    [HttpPost("seed-test-bill")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedTestBill(
        [FromServices] ILeaseRepository leaseRepo,
        [FromServices] ILeaseBillRepository leaseBillRepo,
        [FromServices] IUnitOfWork unitOfWork)
    {
        var leasesResult = await leaseRepo.GetPagedAsync(1, 1);
        var lease = leasesResult.Items.FirstOrDefault();
        if (lease == null) return BadRequest("No leases found in the database. Please create a lease first.");

        var bill = new LeaseBill
        {
            LeaseBillId = Guid.NewGuid(),
            LeaseId = lease.LeaseId,
            BillType = LeaseBillType.Rent,
            DueDate = DateTime.UtcNow.AddDays(5),
            Amount = lease.MonthlyRent > 0 ? lease.MonthlyRent : 15000,
            PendingAmount = lease.MonthlyRent > 0 ? lease.MonthlyRent : 15000
        };

        await leaseBillRepo.AddAsync(bill);
        await unitOfWork.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Test bill created successfully!", 
            leaseBillId = bill.LeaseBillId, 
            tenantId = lease.TenantId 
        });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _paymentService.HandleWebhookAsync(json, signature, ct);
        return Ok();
    }

    [HttpGet("success")]
    [AllowAnonymous]
    public IActionResult Success() => Ok("Payment Successful / Onboarding Complete!");

    [HttpGet("refresh")]
    [AllowAnonymous]
    public IActionResult Refresh() => Ok("Session expired. Please request a new link.");
    
    [HttpGet("checkout-success")]
    [AllowAnonymous]
    public IActionResult CheckoutSuccess() => Ok("Checkout Successful!");

    [HttpGet("checkout-cancel")]
    [AllowAnonymous]
    public IActionResult CheckoutCancel() => Ok("Checkout Cancelled!");
}
