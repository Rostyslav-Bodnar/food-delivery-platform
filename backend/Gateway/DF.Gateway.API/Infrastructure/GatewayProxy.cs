using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Helpers;

namespace DF.Gateway.API.Infrastructure;

public class GatewayProxy(
    HttpClient http,
    InternalGatewayContext internalContext,
    ServiceResolver serviceResolver)
{
    private static readonly HashSet<string> BlockedHeaders =
    [
        "X-Internal-UserId",
        "X-Internal-Timestamp",
        "X-Internal-Nonce",
        "X-Internal-Signature",
        "X-Internal-Key"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // =====================================================
    // PUBLIC GENERIC ENTRY POINT
    // =====================================================
    public async Task<Response<TResponse>> ProxyAsync<TResponse>(HttpContext context)
    {
        try
        {
            var target = serviceResolver.Resolve(context);
            var request = await CreateRequest(context, target);

            using var response = await http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = Deserialize<TResponse>(responseBody);

                return new Response<TResponse>(
                    Success: true,
                    Data: data!,
                    ErrorMassage: null
                );
            }

            var error = TryParseError(responseBody);

            return new Response<TResponse>(
                Success: false,
                Data: default!,
                ErrorMassage: error?.Message ?? "Unknown error"
            );
        }
        catch (Exception ex)
        {
            return new Response<TResponse>(
                Success: false,
                Data: default!,
                ErrorMassage: ex.Message
            );
        }
    }

    // =====================================================
    // REQUEST BUILDER
    // =====================================================
    private async Task<HttpRequestMessage> CreateRequest(HttpContext context, Uri targetUri)
    {
        context.Request.EnableBuffering();

        var request = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            targetUri);

        // headers
        foreach (var header in context.Request.Headers)
        {
            if (BlockedHeaders.Contains(header.Key))
                continue;

            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }


        // body
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            context.Request.Body.Position = 0;

            var memory = new MemoryStream();
            await context.Request.Body.CopyToAsync(memory);
            memory.Position = 0;

            var content = new StreamContent(memory);

            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }

            content.Headers.ContentLength = memory.Length; // ✅ ВАЖЛИВО
            request.Content = content;
            request.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");
            context.Request.Body.Position = 0;
        }

        // internal security headers
        var internalHeaders = internalContext.Build();

        request.Headers.Add("X-Internal-Timestamp", internalHeaders.Timestamp);
        request.Headers.Add("X-Internal-Nonce", internalHeaders.Nonce);
        request.Headers.Add("X-Internal-Signature", internalHeaders.Signature);
        request.Headers.Add("X-Internal-Key", internalHeaders.ApiKey);
        
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            request.Headers.Add("X-Internal-UserId", userId);
        }
        return request;
    }

    // =====================================================
    // DESERIALIZATION
    // =====================================================
    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    // =====================================================
    // ERROR PARSER
    // =====================================================
    private static ServiceErrorResponse? TryParseError(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ServiceErrorResponse>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}