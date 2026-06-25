using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.TenantFinancialProfile;
using EstateSync.Api.Models;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/tenants/me/financial-profile")]
[Authorize(Roles = "Tenant")]
public class TenantFinancialProfileController : ControllerBase
{
    private readonly ITenantFinancialProfileService _service;

    public TenantFinancialProfileController(ITenantFinancialProfileService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPost("draft")]
    public async Task<ActionResult<ApiResponse<TenantFinancialProfileResponse>>> CreateDraft(CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var result = await _service.CreateDraftAsync(tenantId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<TenantFinancialProfileResponse>
        {
            Data = result,
            Message = "Financial profile draft created successfully."
        });
    }

    [HttpPut("{profileId:guid}/documents")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid profileId,
        [FromForm] IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var result = await _service.UploadDocumentAsync(tenantId, profileId, file, documentType, ct);
        return Ok(new ApiResponse<DocumentResponse>
        {
            Data = result,
            Message = "Financial profile document uploaded successfully."
        });
    }

    [HttpPost("{profileId:guid}/submit")]
    public async Task<ActionResult<ApiResponse<object>>> Submit(
        Guid profileId,
        [FromBody] SubmitTenantFinancialProfileRequest request,
        CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        await _service.SubmitAsync(tenantId, profileId, request, ct);
        return Ok(new ApiResponse<object> { Message = "Financial profile submitted for review." });
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantFinancialProfileResponse>>>> GetMy(CancellationToken ct)
    {
        var tenantId = User.GetUserId();
        var result = await _service.GetByTenantIdAsync(tenantId, ct);
        return Ok(new ApiResponse<IEnumerable<TenantFinancialProfileResponse>> { Data = result });
    }
}
