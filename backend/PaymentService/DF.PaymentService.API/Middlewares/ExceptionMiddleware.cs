using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DF.PaymentService.API.Middlewares;

public record ServiceErrorResponse(string Code, string Message, string TraceId);

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private async Task HandleException(HttpContext context, Exception ex)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, error) = MapException(ex, traceId);

        logger.LogError(ex,
            "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
            traceId,
            context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(error, JsonOptions);

        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode, ServiceErrorResponse) MapException(Exception ex, string traceId)
    {
        return ex switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ServiceErrorResponse("UNAUTHORIZED", ex.Message, traceId)),

            AccessViolationException => (
                HttpStatusCode.Forbidden,
                new ServiceErrorResponse("FORBIDDEN", ex.Message, traceId)),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new ServiceErrorResponse("NOT_FOUND", ex.Message, traceId)),

            // Domain rules (e.g. ApplyPartialRefund refusing to exceed remaining balance)
            // surface as InvalidOperationException — these are user errors, not server bugs.
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                new ServiceErrorResponse("DOMAIN_RULE_VIOLATED", ex.Message, traceId)),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                new ServiceErrorResponse("VALIDATION_ERROR", ex.Message, traceId)),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                new ServiceErrorResponse(
                    "CONCURRENCY_CONFLICT",
                    "The resource was modified by another request. Reload and try again.",
                    traceId)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ServiceErrorResponse("INTERNAL_ERROR", "Something went wrong", traceId))
        };
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
