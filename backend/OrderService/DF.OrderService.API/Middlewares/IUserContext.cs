namespace DF.OrderService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
}