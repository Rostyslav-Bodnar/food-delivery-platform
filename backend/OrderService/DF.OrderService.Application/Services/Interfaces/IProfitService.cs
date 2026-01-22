
namespace DF.OrderService.Application.Services.Interfaces;

public interface IProfitService
{
    decimal Calculate(decimal totalPrice, double distanceKm);
}