using DF.UserService.API.Middlewares;

namespace DF.UserService.API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}