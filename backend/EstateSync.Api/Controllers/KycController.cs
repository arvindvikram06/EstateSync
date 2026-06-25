using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Kyc;
using EstateSync.Api.Models;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IKycService _kycService;

    public KycController(IKycService kycService)
    {
        _kycService = kycService ?? throw new ArgumentNullException(nameof(kycService));
    }

    [HttpPost("draft")]
    public async Task<ActionResult<ApiResponse<KycVerificationResponse>>> CreateDraft(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _kycService.CreateDraftAsync(userId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<KycVerificationResponse>
        {
            Data = result,
            Message = "KYC draft created successfully."
        });
    }

    [HttpPut("{kycId:guid}/documents")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid kycId,
        [FromForm] IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _kycService.UploadDocumentAsync(userId, kycId, file, documentType, ct);
        return Ok(new ApiResponse<DocumentResponse>
        {
            Data = result,
            Message = "KYC document uploaded successfully."
        });
    }

    [HttpPost("{kycId:guid}/submit")]
    public async Task<ActionResult<ApiResponse<object>>> Submit(
        Guid kycId,
        [FromBody] SubmitKycRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _kycService.SubmitAsync(userId, kycId, request, ct);
        return Ok(new ApiResponse<object> { Message = "KYC submitted for review." });
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IEnumerable<KycVerificationResponse>>>> GetMy(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _kycService.GetByUserIdAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<KycVerificationResponse>> { Data = result });
    }
}
