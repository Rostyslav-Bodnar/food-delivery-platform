using System.Text.Json;
using DF.OrderService.Domain.Entities;
using DF.OrderService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.API.Filters;

/// <summary>
/// Accepts an Idempotency-Key header; first call runs the action and persists the
/// response, subsequent calls with the same key replay it.
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

        await db.IdempotencyKeys.Where(k => k.ExpiresAt < now).ExecuteDeleteAsync();

        var existing = await db.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == keyHeader);

        if (existing is not null)
        {
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

        var executed = await next();
        if (executed.Canceled || executed.Exception is not null) return;

        var (statusCode, body) = ExtractResponse(executed.Result);
        if (statusCode is not (>= 200 and < 300)) return;

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

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { /* concurrent insert lost the race; tolerated */ }
    }

    private static (int statusCode, string? body) ExtractResponse(IActionResult? result) => result switch
    {
        ObjectResult or => (or.StatusCode ?? 200, JsonSerializer.Serialize(or.Value, JsonOptions)),
        JsonResult jr => (jr.StatusCode ?? 200, JsonSerializer.Serialize(jr.Value, JsonOptions)),
        StatusCodeResult sc => (sc.StatusCode, null),
        _ => (200, null)
    };
}
