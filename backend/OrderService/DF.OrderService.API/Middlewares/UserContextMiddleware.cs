namespace DF.OrderService.API.Middlewares;

public class UserContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var accountId = context.Request.Headers["X-Internal-AccountId"].FirstOrDefault();
        if (Guid.TryParse(accountId, out var accId))
        {
            context.Items["AccountId"] = accId;
        }
        var userId = context.Request.Headers["X-Internal-UserId"].FirstOrDefault();
        var effectiveId = !string.IsNullOrWhiteSpace(accountId) ? accountId : userId;

        if (Guid.TryParse(effectiveId, out var id))
        {
            context.Items["UserId"] = id;
        }

        var accountType = context.Request.Headers["X-Internal-AccountType"].FirstOrDefault();
        var role = !string.IsNullOrWhiteSpace(accountType)
            ? accountType
            : context.Request.Headers["X-Internal-Role"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(accountType))
        {
            context.Items["AccountType"] = accountType;
        }

        await next(context);
    }
}
