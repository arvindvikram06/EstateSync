using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Models;

using EstateSync.Api.Extensions;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IPropertyImageService _propertyImageService;
    private readonly IPropertyAmenityService _propertyAmenityService;
    private readonly IPropertyApprovalService _propertyApprovalService;
    private readonly IPropertyDocumentService _propertyDocumentService;

    public PropertiesController(
        IPropertyService propertyService,
        IPropertyImageService propertyImageService,
        IPropertyAmenityService propertyAmenityService,
        IPropertyApprovalService propertyApprovalService,
        IPropertyDocumentService propertyDocumentService)
    {
        _propertyService = propertyService ?? throw new ArgumentNullException(nameof(propertyService));
        _propertyImageService = propertyImageService ?? throw new ArgumentNullException(nameof(propertyImageService));
        _propertyAmenityService = propertyAmenityService ?? throw new ArgumentNullException(nameof(propertyAmenityService));
        _propertyApprovalService = propertyApprovalService ?? throw new ArgumentNullException(nameof(propertyApprovalService));
        _propertyDocumentService = propertyDocumentService ?? throw new ArgumentNullException(nameof(propertyDocumentService));
    }

    // search (public)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<PropertySummaryResponse>>> Search(
        [FromQuery] PropertySearchRequest request,
        CancellationToken ct)
    {
        var result = await _propertyService.SearchAsync(request, ct);
        return Ok(result);
    }

    // get by id (public)
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PropertyDetailResponse>>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await _propertyService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<PropertyDetailResponse> { Data = result });
    }

    [HttpGet("me")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<List<PropertyDetailResponse>>>> GetMyProperties(
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _propertyService.GetByOwnerIdAsync(ownerId, ct);
        return Ok(new ApiResponse<List<PropertyDetailResponse>> { Data = result });
    }

    // Create Property (Owner)
    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<PropertyDetailResponse>>> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _propertyService.CreateAsync(request, ownerId, ct);
        
        var response = new ApiResponse<PropertyDetailResponse>
        {
            Data = result,
            Message = "property created successfully"
        };

        return CreatedAtAction(nameof(GetById), new { id = result.PropertyId }, response);
    }
    
    // Update Property (Owner)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<PropertyDetailResponse>>> Update(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var result = await _propertyService.UpdateAsync(id, request, ownerId, ct);
        return Ok(new ApiResponse<PropertyDetailResponse> { Data = result });
    }

    //Delete Property (Owner/Admin)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        await _propertyService.DeleteAsync(id, userId, isAdmin, ct);
        return NoContent();
    }

    // Submit for Approval (Owner)
    [HttpPut("{id:guid}/submit")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> Submit(Guid id, CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyApprovalService.SubmitForApprovalAsync(id, ownerId, ct);
        return Ok(new ApiResponse<object> { Data = new { status = "PendingApproval" } });
    }

    // Property Images (Owner)
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> UploadImages(
        Guid id,
        [FromForm] List<IFormFile> files,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var imageIds = await _propertyImageService.UploadImagesAsync(id, files, ownerId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object> { Data = new { imageIds } });
    }

    // Delete Image (Owner)
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyImageService.DeleteImageAsync(id, imageId, ownerId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/amenities")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> AddAmenities(
        Guid id,
        [FromBody] AddAmenitiesRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyAmenityService.AddAmenitiesAsync(id, request.AmenityNames, ownerId, ct);
        return Ok(new ApiResponse<object> { Message = "Amenities added successfully." });
    }

    [HttpPut("{id:guid}/amenities")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAmenities(
        Guid id,
        [FromBody] AddAmenitiesRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyAmenityService.UpdateAmenitiesAsync(id, request.AmenityNames, ownerId, ct);
        return Ok(new ApiResponse<object> { Message = "Amenities list updated successfully." });
    }

    [HttpDelete("{id:guid}/amenities")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAmenities(
        Guid id,
        [FromBody] AddAmenitiesRequest request,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyAmenityService.DeleteAmenitiesAsync(id, request.AmenityNames, ownerId, ct);
        return Ok(new ApiResponse<object> { Message = "Amenities removed successfully." });
    }

    // [HttpGet("{id:guid}/reviews")]
    // [AllowAnonymous]
    // public async Task<ActionResult<ApiResponse<PagedResponse<ReviewResponse>>>> GetReviews(
    //     Guid id,
    //     [FromQuery] int page = 1,
    //     [FromQuery] int limit = 20,
    //     CancellationToken ct = default)
    // {
    //     var result = await _propertyService.GetReviewsAsync(id, page, limit, ct);
    //     return Ok(new ApiResponse<PagedResponse<ReviewResponse>> { Data = result });
    // }

    // Property  Ownership Documents (Owner)
    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        Guid id,
        IFormFile file,
        DocumentType documentType,
        CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        var response = await _propertyDocumentService.UploadDocumentAsync(id, file, documentType, ownerId, ct);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<DocumentResponse> { Data = response });
    }

    // Delete Document (Owner)
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        var ownerId = User.GetUserId();
        await _propertyDocumentService.DeleteDocumentAsync(id, documentId, ownerId, ct);
        return NoContent();
    }

    // Get Property Documents (Owner/Admin)
    [HttpGet("{id:guid}/documents")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<List<DocumentResponse>>>> GetPropertyDocuments(
        Guid id,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var response = await _propertyDocumentService.GetPropertyDocumentsAsync(id, userId, isAdmin, ct);
        return Ok(new ApiResponse<List<DocumentResponse>> { Data = response });
    }

}

public record AddAmenitiesRequest(List<string> AmenityNames);
