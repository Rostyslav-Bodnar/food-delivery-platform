namespace DF.PaymentService.API.Middlewares;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId =>
        accessor.HttpContext?.Items["UserId"] is Guid id ? id : Guid.Empty;

    public string? Role =>
        accessor.HttpContext?.Items["UserRole"] as string;

    public bool IsAuthenticated => UserId != Guid.Empty;
}
