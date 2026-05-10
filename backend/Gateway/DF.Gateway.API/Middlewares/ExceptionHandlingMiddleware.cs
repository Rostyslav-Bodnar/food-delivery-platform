using System.Net;
using System.Text.Json;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Exceptions;

namespace DF.Gateway.API.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await WriteErrorAsync(
                context,
                ex.StatusCode,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            // ❗ fallback для 500
            await WriteErrorAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                "Internal server error"
            );
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new Response<object>(
            Success: false,
            Data: null,
            ErrorMassage: message
        );

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}