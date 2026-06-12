using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.TenantVerification;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Interfaces.Services.TenantVerification;
using EstateSync.Api.Models;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantVerificationsController : ControllerBase
{
    private readonly ITenantVerificationService _verificationService;

    public TenantVerificationsController(ITenantVerificationService verificationService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    [HttpPost("applications/{applicationId:guid}/initiate")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<TenantVerificationResponse>>> Initiate(Guid applicationId, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var result = await _verificationService.InitiateVerificationAsync(applicationId, tenantId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<TenantVerificationResponse>
        {
            Data = result,
            Message = "Tenant verification request initiated successfully."
        });
    }

    [HttpPut("{requestId:guid}/documents")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid requestId,
        IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var result = await _verificationService.UploadDocumentAsync(requestId, file, documentType, tenantId, ct);
        return Ok(new ApiResponse<DocumentResponse>
        {
            Data = result,
            Message = "Document uploaded successfully."
        });
    }

    [HttpPost("{requestId:guid}/submit")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ApiResponse<object>>> Submit(Guid requestId, CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        await _verificationService.SubmitVerificationAsync(requestId, tenantId, ct);
        return Ok(new ApiResponse<object>
        {
            Message = "Tenant verification request submitted successfully."
        });
    }

    [HttpPost("{requestId:guid}/review")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<TenantVerificationResponse>>> Review(
        Guid requestId,
        [FromBody] ReviewTenantVerificationRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _verificationService.ReviewVerificationAsync(requestId, request, ownerId, ct);
        return Ok(new ApiResponse<TenantVerificationResponse>
        {
            Data = result,
            Message = request.Approve ? "Tenant verification approved." : "Tenant verification rejected."
        });
    }

    [HttpGet("{requestId:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TenantVerificationResponse>>> GetById(Guid requestId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _verificationService.GetByIdAsync(requestId, userId, isAdmin, ct);
        return Ok(new ApiResponse<TenantVerificationResponse>
        {
            Data = result
        });
    }

    [HttpGet("submitted")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<PagedResponse<TenantVerificationResponse>>> GetSubmittedVerificationRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.GetPagedTenantRequestsAsync(userId, status, page, limit, ct);
        return Ok(result);
    }

    [HttpGet("incoming")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<PagedResponse<TenantVerificationResponse>>> GetIncomingVerificationRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.GetPagedOwnerRequestsAsync(userId, status, page, limit, ct);
        return Ok(result);
    }

}
