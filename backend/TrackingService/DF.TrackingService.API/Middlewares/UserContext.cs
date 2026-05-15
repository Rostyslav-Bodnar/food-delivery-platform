namespace DF.TrackingService.API.Middlewares;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId =>
        accessor.HttpContext?.Items["UserId"] is Guid id
            ? id
            : Guid.Empty;
}