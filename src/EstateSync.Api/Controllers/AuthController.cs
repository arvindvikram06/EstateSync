using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Auth;
using EstateSync.Api.Interfaces.Services.Auth;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
    {
        var (auth, refreshToken) = await _authService.RegisterAsync(request);
        SetRefreshTokenCookie(refreshToken);

        var data = new
        {
            userId = auth.UserId,
            accessToken = auth.AccessToken,
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse<object>.Ok(data,message: "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginRequest request)
    {
        var (auth, refreshToken) = await _authService.LoginAsync(request);
        SetRefreshTokenCookie(refreshToken);

        var data = new
        {
            accessToken = auth.AccessToken,
            user = new
            {
                id = auth.UserId,
                fullName = auth.FullName,
                roles = auth.Roles
            }
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<object>>> RefreshToken()
    {
        var token = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(ApiResponse<object>.Fail("Refresh token is required."));
        }

        var (auth, newRefreshToken) = await _authService.RefreshTokenAsync(token);
        SetRefreshTokenCookie(newRefreshToken);

        var data = new
        {
            accessToken = auth.AccessToken
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    // [HttpPost("forgot-password")]
    // public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    // {
    //     await _authService.ForgotPasswordAsync(request);
    //     return Ok(ApiResponse<object>.Ok(new { message = "If the email is registered, a reset link has been sent." }));
    // }

    // [HttpPost("reset-password")]
    // public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    // {
    //     await _authService.ResetPasswordAsync(request);
    //     return Ok(ApiResponse<object>.Ok(new { message = "Password reset successful." }));
    // }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
