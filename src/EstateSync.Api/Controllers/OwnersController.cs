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
using EstateSync.Api.DTOs.OwnerVerification;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.OwnerVerification;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OwnersController : ControllerBase
{
    private readonly IOwnerVerificationService _verificationService;

    public OwnersController(IOwnerVerificationService verificationService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    [HttpPost("me/verification-request/draft")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OwnerVerificationResponse>>> CreateDraft(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.CreateDraftVerificationRequestAsync(userId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OwnerVerificationResponse>
        {
            Data = result,
            Message = "Draft verification request created successfully."
        });
    }

    [HttpPut("me/verification-request/{requestId:guid}/documents")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid requestId,
        IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.UploadVerificationDocumentAsync(userId, requestId, file, documentType, ct);
        return Ok(new ApiResponse<DocumentResponse>
        {
            Data = result,
            Message = "Verification document uploaded successfully."
        });
    }

    [HttpPost("me/verification-request/{requestId:guid}/submit")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SubmitRequest(
        Guid requestId,
        [FromBody] SubmitOwnerVerificationRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _verificationService.SubmitVerificationRequestAsync(userId, requestId, request, ct);
        return Ok(new ApiResponse<object>
        {
            Message = "Owner verification request submitted successfully."
        });
    }

    [HttpGet("me/verification-requests")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<OwnerVerificationResponse>>>> GetVerificationRequests(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _verificationService.GetOwnerVerificationRequestsByUserIdAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<OwnerVerificationResponse>> { Data = result });
    }

}
