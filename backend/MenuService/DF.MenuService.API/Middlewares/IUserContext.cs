namespace DF.MenuService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
}