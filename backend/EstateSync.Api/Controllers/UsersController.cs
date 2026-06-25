using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
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

    [HttpPost("me/become-owner")]
    public async Task<ActionResult<ApiResponse<object>>> BecomeOwner(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var message = await _userService.BecomeOwnerAsync(userId, ct);
        return Ok(new ApiResponse<object> { Message = message });
    }
}
