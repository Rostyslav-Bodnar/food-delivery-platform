    
namespace DF.UserService.API.Helpers;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext context)
    {
        var value = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
            throw new UnauthorizedAccessException("X-User-Id header missing");

        return Guid.Parse(value);
    }
}