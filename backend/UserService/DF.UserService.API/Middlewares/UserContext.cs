using DF.UserService.API.Helpers;

namespace DF.UserService.API.Middlewares;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId =>
        accessor.HttpContext?.Items["UserId"] is Guid id
            ? id
            : Guid.Empty;
}