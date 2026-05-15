using System.Net;
using System.Text.Json;
using DF.Contracts.Gateway.Responses;

namespace DF.Gateway.API.Middlewares;

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
            await Handle(context, ex);
        }
    }

    private async Task Handle(HttpContext context, Exception ex)
    {
        var traceId = context.TraceIdentifier;

        var (status, error) = Map(ex, traceId);

        logger.LogError(ex,
            "Gateway error | TraceId: {TraceId} | Path: {Path}",
            traceId,
            context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var json = JsonSerializer.Serialize(error, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode, ServiceErrorResponse) Map(Exception ex, string traceId)
    {
        return ex switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ServiceErrorResponse("UNAUTHORIZED", ex.Message, traceId)
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new ServiceErrorResponse("NOT_FOUND", ex.Message, traceId)
            ),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                new ServiceErrorResponse("VALIDATION_ERROR", ex.Message, traceId)
            ),

            HttpRequestException => (
                HttpStatusCode.BadGateway,
                new ServiceErrorResponse("BAD_GATEWAY", "Downstream service failed", traceId)
            ),

            TaskCanceledException => (
                HttpStatusCode.GatewayTimeout,
                new ServiceErrorResponse("TIMEOUT", "Request timeout", traceId)
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                new ServiceErrorResponse("INTERNAL_ERROR", "Something went wrong", traceId)
            )
        };
    }
}