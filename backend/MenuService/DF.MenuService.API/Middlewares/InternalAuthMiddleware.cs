using DF.MenuService.API.Helpers;

namespace DF.MenuService.API.Middlewares;

public class InternalAuthMiddleware(
    RequestDelegate next,
    IConfiguration config
)
{
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // ❌ SKIP AUTH
        if (
            path is not null &&
            path.Contains("/api/auth")
        )
        {
            await next(context);
            return;
        }

        var timestamp =
            context.Request.Headers[
                InternalAuthConstants.TimestampHeader
            ].FirstOrDefault();

        var nonce =
            context.Request.Headers[
                InternalAuthConstants.NonceHeader
            ].FirstOrDefault();

        var signature =
            context.Request.Headers[
                InternalAuthConstants.SignatureHeader
            ].FirstOrDefault();

        var apiKey =
            context.Request.Headers[
                InternalAuthConstants.ApiKeyHeader
            ].FirstOrDefault();

        if (
            timestamp is null ||
            nonce is null ||
            signature is null ||
            apiKey is null
        )
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing internal headers");
            return;
        }

        // API KEY CHECK — timing-safe to defeat side-channel enumeration.
        var expectedKey = config["Internal:ApiKey"] ?? string.Empty;
        var providedBytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedKey);
        if (providedBytes.Length != expectedBytes.Length
            || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        var signer =
            new InternalAuthSigner(config);

        if (!signer.Validate(timestamp, nonce, signature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature");
            return;
        }

        // replay protection
        var time =
            DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(timestamp)
            );

        if (
            DateTimeOffset.UtcNow - time >
            TimeSpan.FromMinutes(5)
        )
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Expired request");
            return;
        }

        await next(context);
    }
}