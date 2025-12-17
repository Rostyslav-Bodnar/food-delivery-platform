using DF.OrderService.Application.Services.Interfaces;

namespace DF.OrderService.Application.Services;

public class ProfitService(double ratePerKm = 1.2) : IProfitService
{
    public decimal Calculate(decimal totalPrice, double distanceKm)
    {
        // 25% від ціни замовлення
        var baseShare = (double)(totalPrice * 0.25m);

        // додаємо бонус за дистанцію
        var distanceBonus = distanceKm * ratePerKm;

        var profit = baseShare + distanceBonus;
        return (decimal)profit;
    }
}
