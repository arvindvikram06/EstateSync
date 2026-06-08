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
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.OwnerVerification;

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
    public async Task<ActionResult<ApiResponse<object>>> CreateDraft(CancellationToken ct)
    {
        var userId = GetUserId();
        var requestId = await _verificationService.CreateDraftVerificationRequestAsync(userId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Data = new { requestId },
            Message = "Draft verification request created successfully."
        });
    }

    [HttpPost("me/verification-request/{requestId:guid}/documents")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid requestId,
        IFormFile file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var userId = GetUserId();
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
        CancellationToken ct)
    {
        var userId = GetUserId();
        await _verificationService.SubmitVerificationRequestAsync(userId, requestId, ct);
        return Ok(new ApiResponse<object>
        {
            Message = "Owner verification request submitted successfully."
        });
    }

    [HttpGet("me/verification-requests")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<VerificationRequestResponse>>>> GetVerificationRequests(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _verificationService.GetUserVerificationRequestsAsync(userId, ct);
        return Ok(new ApiResponse<IEnumerable<VerificationRequestResponse>> { Data = result });
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }
}
