using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
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

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        AppException appEx => ((int)appEx.StatusCode, appEx.Message),
        ArgumentNullException => (StatusCodes.Status400BadRequest, "Invalid argument provided."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument provided."),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access."),
        Stripe.StripeException => (StatusCodes.Status400BadRequest, "Stripe API or webhook validation error."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred on the server.")
    };

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };

    private static string? GetSafeErrorMessage(Exception exception, IHostEnvironment env)
    {
        if (env.IsDevelopment()) return exception.Message;
        return exception is AppException ? null : "Please contact support if the issue persists.";
    }
}
