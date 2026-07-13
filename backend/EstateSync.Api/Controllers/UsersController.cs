using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Extensions;
using EstateSync.Api.Interfaces.Services.Users;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me/profile")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetProfile(CancellationToken ct)
    {
        var profile = await _userService.GetProfileAsync(User.GetUserId(), ct);
        return Ok(new ApiResponse<UserProfileResponse> { Data = profile });
    }

    [HttpPut("me/profile")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var profile = await _userService.UpdateProfileAsync(User.GetUserId(), request, ct);
        return Ok(new ApiResponse<UserProfileResponse> { Data = profile, Message = "Profile updated." });
    }

    [HttpPut("me/change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _userService.ChangePasswordAsync(User.GetUserId(), request, ct);
        return Ok(new ApiResponse<object> { Message = "Password changed successfully." });
    }

    [HttpPost("me/become-owner")]
    public async Task<ActionResult<ApiResponse<object>>> BecomeOwner([FromBody] BecomeOwnerRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var message = await _userService.BecomeOwnerAsync(userId, request, ct);
        return Ok(new ApiResponse<object> { Message = message });
    }
}
