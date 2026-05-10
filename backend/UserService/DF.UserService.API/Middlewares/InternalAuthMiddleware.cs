using DF.UserService.API.Helpers;

namespace DF.UserService.API.Middlewares;

public class InternalAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task Invoke(HttpContext context)
    {
        var userId = context.Request.Headers[InternalAuthConstants.UserIdHeader].FirstOrDefault();
        var timestamp = context.Request.Headers[InternalAuthConstants.TimestampHeader].FirstOrDefault();
        var signature = context.Request.Headers[InternalAuthConstants.SignatureHeader].FirstOrDefault();

        if (userId is null || timestamp is null || signature is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing internal auth headers");
            return;
        }

        var signer = new InternalAuthSigner(config["Internal:ApiKey"]!);

        if (!signer.Validate(userId, timestamp, signature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid internal signature");
            return;
        }

        // ⛑ anti replay protection (5 min)
        var time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));
        if (DateTimeOffset.UtcNow - time > TimeSpan.FromMinutes(5))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Request expired");
            return;
        }

        if (Guid.TryParse(userId, out var id))
        {
            context.Items["UserId"] = id;
        }
        else
        {
            context.Items["UserId"] = Guid.Empty; 
        }

        await next(context);
    }
}