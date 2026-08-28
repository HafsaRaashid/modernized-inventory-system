using System.Net;
using System.Text.Json;

namespace InventoryTrackingSystem.Api.Middleware;

/// <summary>
/// Foundation-level error-handling convention (error-handling pillar): any
/// unhandled exception is caught here, logged with its trace id, and
/// reshaped into a stable JSON error envelope for the caller. The shape of
/// the envelope is the only thing this class decides — no business rule
/// lives here.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                traceId = context.TraceIdentifier,
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
