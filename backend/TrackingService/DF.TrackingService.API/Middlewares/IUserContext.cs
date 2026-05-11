namespace DF.TrackingService.API.Middlewares;

public interface IUserContext
{
    Guid UserId { get; }
}