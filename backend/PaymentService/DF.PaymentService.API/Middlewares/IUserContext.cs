namespace DF.PaymentService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
