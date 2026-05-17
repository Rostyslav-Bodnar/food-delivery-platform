using System.Net;
using System.Text.Json;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Contracts.Exceptions;

namespace DF.UserService.API.Middlewares;

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
            // 🔐 AUTH
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ServiceErrorResponse(
                    "UNAUTHORIZED",
                    ex.Message,
                    traceId)
            ),

            // 🚫 FORBIDDEN (опційно)
            AccessViolationException => (
                HttpStatusCode.Forbidden,
                new ServiceErrorResponse(
                    "FORBIDDEN",
                    ex.Message,
                    traceId)
            ),

            // 🔍 NOT FOUND
            NotFoundException => (
                HttpStatusCode.NotFound,
                new ServiceErrorResponse(
                    "NOT_FOUND",
                    ex.Message,
                    traceId)
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new ServiceErrorResponse(
                    "NOT_FOUND",
                    ex.Message,
                    traceId)
            ),

            // 📉 VALIDATION
            ArgumentException => (
                HttpStatusCode.BadRequest,
                new ServiceErrorResponse(
                    "VALIDATION_ERROR",
                    ex.Message,
                    traceId)
            ),

            // 🔥 DEFAULT
            _ => (
                HttpStatusCode.InternalServerError,
                new ServiceErrorResponse(
                    "INTERNAL_ERROR",
                    "Something went wrong",
                    traceId)
            )
        };
    }
}