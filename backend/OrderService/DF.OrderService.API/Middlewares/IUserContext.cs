namespace DF.OrderService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
    string? AccountType { get; }
    Guid AccountId { get; }
    bool IsAuthenticated { get; }
}
