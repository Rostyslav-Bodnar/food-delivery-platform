namespace DF.OrderService.Application.Services.Interfaces;


public interface IDistanceService
{
    Task<double> GetDistanceKmAsync(double fromLat, double fromLon, double toLat, double toLon);
}

