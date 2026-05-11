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

        await next(context);
    }
}