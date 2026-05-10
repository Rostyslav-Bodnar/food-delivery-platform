namespace DF.UserService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
}