using System.Text.Json;
using DF.MenuService.Domain.Entities;
using DF.MenuService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.API.Filters;

/// <summary>
/// Activates Idempotency-Key support on the decorated action.
/// Caller passes "Idempotency-Key: &lt;uuid&gt;" — the first request runs the handler and
/// stores the response; subsequent requests with the same key + path + method replay it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan KeyTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var keyHeader = http.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(keyHeader))
        {
            // No key supplied — endpoint supports idempotency, doesn't require it.
            await next();
            return;
        }

        if (keyHeader.Length > 100)
        {
            context.Result = new BadRequestObjectResult(new { error = "Idempotency-Key too long (max 100 chars)" });
            return;
        }

        var db = http.RequestServices.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var path = http.Request.Path.Value ?? string.Empty;
        var method = http.Request.Method;

        // Best-effort sweep of expired keys; cheap and avoids unbounded growth.
        await db.IdempotencyKeys.Where(k => k.ExpiresAt < now).ExecuteDeleteAsync();

        var existing = await db.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == keyHeader);

        if (existing is not null)
        {
            // Reusing a key with a different request shape is an error (Stripe convention).
            if (existing.Method != method || existing.Path != path)
            {
                context.Result = new ConflictObjectResult(new
                {
                    error = "IDEMPOTENCY_KEY_REUSED",
                    message = "This idempotency key was already used with a different method/path."
                });
                return;
            }

            context.Result = new ContentResult
            {
                StatusCode = existing.StatusCode,
                Content = existing.ResponseBody,
                ContentType = "application/json"
            };
            return;
        }

        // First time: run the action.
        var executed = await next();

        if (executed.Canceled || executed.Exception is not null)
            return;

        // Only persist successful responses; let errors retry freely.
        var (statusCode, body) = ExtractResponse(executed.Result);
        if (statusCode is not (>= 200 and < 300))
            return;

        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = keyHeader,
            Method = method,
            Path = path,
            StatusCode = statusCode,
            ResponseBody = body,
            CreatedAt = now,
            ExpiresAt = now.Add(KeyTtl)
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrent insert lost the race — the other writer's response will be served on replay.
        }
    }

    private static (int statusCode, string? body) ExtractResponse(IActionResult? result) => result switch
    {
        ObjectResult or => (or.StatusCode ?? 200, JsonSerializer.Serialize(or.Value, JsonOptions)),
        JsonResult jr => (jr.StatusCode ?? 200, JsonSerializer.Serialize(jr.Value, JsonOptions)),
        StatusCodeResult sc => (sc.StatusCode, null),
        _ => (200, null)
    };
}
