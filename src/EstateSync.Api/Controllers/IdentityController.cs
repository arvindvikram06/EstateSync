using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.IdentityVerification;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityVerificationService _verificationService;

    public IdentityController(IIdentityVerificationService verificationService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    [HttpPost("verification-request/draft")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IdentityVerificationResponse>>> CreateDraft(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.CreateDraftIdentityRequestAsync(userId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<IdentityVerificationResponse>
        {
            Data = result,
            Message = "Draft identity verification request created successfully."
        });
    }

    [HttpPut("verification-request/{requestId:guid}/documents")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid requestId,
        IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.UploadIdentityDocumentAsync(userId, requestId, file, documentType, ct);
        return Ok(new ApiResponse<DocumentResponse>
        {
            Data = result,
            Message = "Verification document uploaded successfully."
        });
    }

    [HttpPost("verification-request/{requestId:guid}/submit")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SubmitRequest(
        Guid requestId,
        [FromBody] SubmitIdentityVerificationRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _verificationService.SubmitIdentityRequestAsync(userId, requestId, request, ct);
        return Ok(new ApiResponse<object>
        {
            Message = "Identity verification request submitted successfully."
        });
    }

    [HttpGet("verification-requests")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<IdentityVerificationResponse>>>> GetVerificationRequests(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.GetIdentityRequestsByUserIdAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<IdentityVerificationResponse>> { Data = result });
    }

}
