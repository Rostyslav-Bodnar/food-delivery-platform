namespace DF.OrderService.API.Middlewares;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId =>
        accessor.HttpContext?.Items["UserId"] is Guid id ? id : Guid.Empty;

    public string? AccountType =>
        accessor.HttpContext?.Items["AccountType"] as string;
    public Guid AccountId =>
        accessor.HttpContext?.Items["AccountId"] is Guid id ? id : Guid.Empty;
    public bool IsAuthenticated => UserId != Guid.Empty;
}
