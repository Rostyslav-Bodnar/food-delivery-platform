namespace DF.TrackingService.API.Middlewares;

public class UserContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var userId = context.Request.Headers["X-Internal-UserId"].FirstOrDefault();
        if (Guid.TryParse(userId, out var id))
        {
            context.Items["UserId"] = id;
        }

        var accountId = context.Request.Headers["X-Internal-AccountId"].FirstOrDefault();
        if (Guid.TryParse(accountId, out var accId))
        {
            context.Items["AccountId"] = accId;
        }

        var accountType = context.Request.Headers["X-Internal-AccountType"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            context.Items["AccountType"] = accountType;
        }

        await next(context);
    }
}
