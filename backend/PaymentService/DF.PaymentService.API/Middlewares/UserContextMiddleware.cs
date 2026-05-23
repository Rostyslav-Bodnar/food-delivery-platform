namespace DF.PaymentService.API.Middlewares;

public class UserContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var userId = context.Request.Headers["X-Internal-UserId"].FirstOrDefault();
        if (Guid.TryParse(userId, out var id))
        {
            context.Items["UserId"] = id;
        }

        var role = context.Request.Headers["X-Internal-Role"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(role))
        {
            context.Items["UserRole"] = role;
        }

        await next(context);
    }
}
