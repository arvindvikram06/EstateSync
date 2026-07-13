using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    // User Registration Endpoint
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
    {
        await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Message = "Registration successful. Check your email for the verification link."
        });
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authService.VerifyEmailAsync(request.Token);
        return Ok(new ApiResponse<object> { Message = "Email verified successfully. You can now sign in." });
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        await _authService.ResendVerificationAsync(request.Email);
        return Ok(new ApiResponse<object> { Message = "If that email is registered and unverified, a new verification link has been sent." });
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

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var token = Request.Cookies["refreshToken"];

        await _authService.LogoutAsync(token);

        // Delete the cookie using attributes that match how it was set, so the browser removes it.
        var isProduction = _env.IsProduction();
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax
        });

        return Ok(ApiResponse<object>.Ok(new { }, message: "Logged out."));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new ApiResponse<object> { Message = "If the email is registered, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new ApiResponse<object> { Message = "Password reset successful. You can now sign in with your new password." });
    }

    private void SetRefreshTokenCookie(string token)
    {
        var isProduction = _env.IsProduction();
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
