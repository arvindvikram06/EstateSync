using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Stripe;
using EstateSync.Api.Exceptions;

namespace EstateSync.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, title) = MapException(exception);

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Server Error occurred. TraceId: {TraceId}", traceId);
        }
        else
        {
            logger.LogWarning("Client Error ({StatusCode}): {Message}. TraceId: {TraceId}", statusCode, exception.Message, traceId);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = GetProblemType(statusCode),
            Detail = GetSafeErrorMessage(exception, env),
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (exception is ValidationException ve)
            problemDetails.Extensions["errors"] = ve.Errors;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        AppException appEx => ((int)appEx.StatusCode, GetDefaultTitleForStatus((int)appEx.StatusCode)),
        ArgumentNullException => (StatusCodes.Status500InternalServerError, "An internal server error occurred."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument provided."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access."),
        StripeException => (StatusCodes.Status400BadRequest, "Stripe API or webhook validation error."),
        DbUpdateException => (StatusCodes.Status500InternalServerError, "A database error occurred while saving your changes."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred on the server.")
    };

    private static string GetDefaultTitleForStatus(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Validation Error",
        _ => "Error"
    };

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    private static string? GetSafeErrorMessage(Exception exception, IHostEnvironment env)
    {
        if (exception is AppException) return exception.Message;
        if (env.IsDevelopment()) return exception.Message;
        return "Please contact support if the issue persists.";
    }
}
