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

    // User Registration Endpoint
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var (auth, refreshToken) = await _authService.RegisterAsync(request);

        // Set the refresh token in an HttpOnly cookie
        SetRefreshTokenCookie(refreshToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<AuthResponse>.Ok(auth, message: "Registration successful."));
    }

    // User Login Endpoint
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var (auth, refreshToken) = await _authService.LoginAsync(request);

        //set the refresh token in an HttpOnly cookie
        SetRefreshTokenCookie(refreshToken);

        return Ok(ApiResponse<AuthResponse>.Ok(auth));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken()
    {
        var token = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Refresh token is required."));
        }

        var (auth, newRefreshToken) = await _authService.RefreshTokenAsync(token);
        SetRefreshTokenCookie(newRefreshToken);

        return Ok(ApiResponse<AuthResponse>.Ok(auth));
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
