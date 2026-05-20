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
        "Host",
        "Content-Length",

        "X-Internal-UserId",
        "X-Internal-Role",
        "X-Internal-AccountId",
        "X-Internal-AccountType",
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
    // PUBLIC ENTRY
    // =====================================================

    public async Task<Response<TResponse>> ProxyAsync<TResponse>(
        HttpContext context)
    {
        try
        {
            var targetUri = serviceResolver.Resolve(context);

            using var request =
                await CreateRequestAsync(context, targetUri);

            using var response = await http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    context.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                if (typeof(TResponse) == typeof(string))
                {
                    return new Response<TResponse>(
                        true,
                        (TResponse)(object)responseBody,
                        null);
                }

                var data = Deserialize<TResponse>(responseBody);

                return new Response<TResponse>(
                    true,
                    data!,
                    null);
            }

            var error = TryParseError(responseBody);

            return new Response<TResponse>(
                false,
                default!,
                error?.Message ?? responseBody);
        }
        catch (Exception ex)
        {
            return new Response<TResponse>(
                false,
                default!,
                ex.ToString());
        }
    }

    // =====================================================
    // REQUEST BUILDER
    // =====================================================

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpContext context,
        Uri targetUri)
    {
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetUri
        };

        // =================================================
        // COPY BODY (WORKS FOR JSON / FORM / FILES)
        // =================================================

        if (HasBody(context.Request))
        {
            context.Request.EnableBuffering();

            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }

            var memory = new MemoryStream();

            await context.Request.Body.CopyToAsync(
                memory,
                context.RequestAborted);

            memory.Position = 0;

            request.Content = new StreamContent(memory);
        }

        // =================================================
        // COPY HEADERS
        // =================================================

        foreach (var header in context.Request.Headers)
        {
            if (BlockedHeaders.Contains(header.Key))
                continue;

            // CONTENT HEADERS
            if (header.Key.StartsWith(
                    "Content-",
                    StringComparison.OrdinalIgnoreCase))
            {
                request.Content?.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value.ToArray());

                continue;
            }

            // REQUEST HEADERS
            request.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value.ToArray());
        }

        // =================================================
        // INTERNAL HEADERS
        // =================================================

        var internalHeaders = internalContext.Build();

        request.Headers.TryAddWithoutValidation(
            "X-Internal-Timestamp",
            internalHeaders.Timestamp);

        request.Headers.TryAddWithoutValidation(
            "X-Internal-Nonce",
            internalHeaders.Nonce);

        request.Headers.TryAddWithoutValidation(
            "X-Internal-Signature",
            internalHeaders.Signature);

        request.Headers.TryAddWithoutValidation(
            "X-Internal-Key",
            internalHeaders.ApiKey);

        var userId =
            context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            request.Headers.TryAddWithoutValidation(
                "X-Internal-UserId",
                userId);
        }

        var role =
            context.User.FindFirst("role")?.Value
            ?? context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrWhiteSpace(role))
        {
            request.Headers.TryAddWithoutValidation(
                "X-Internal-Role",
                role);
        }

        // Forward the caller's active account so downstream services can authorize
        // mutations against it (e.g. TrackingService verifies the business owns the
        // location being modified).
        var accountId = context.User.FindFirst("account_id")?.Value;
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            request.Headers.TryAddWithoutValidation(
                "X-Internal-AccountId",
                accountId);
        }

        var accountType = context.User.FindFirst("account_type")?.Value;
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            request.Headers.TryAddWithoutValidation(
                "X-Internal-AccountType",
                accountType);
        }

        return request;
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private static bool HasBody(HttpRequest request)
    {
        return request.ContentLength > 0
               || request.Headers.ContainsKey("Transfer-Encoding");
    }

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(
            json,
            JsonOptions);
    }

    private static ServiceErrorResponse? TryParseError(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ServiceErrorResponse>(
                json,
                JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}