namespace DF.OrderService.Application.Services;

// Calculates the delivery fee (charged to the customer). The platform's profit is
// computed elsewhere as DeliveryFee - CourierFee - paymentProcessingFee.
public static class DeliveryFeeCalculator
{
    private const decimal RatePerKm = 1.20m;
    private const decimal BaseSharePercent = 0.25m;

    public static decimal Calculate(decimal totalPrice, double distanceKm)
    {
        var baseShare = totalPrice * BaseSharePercent;
        var distanceBonus = RatePerKm * (decimal)distanceKm;
        return decimal.Round(baseShare + distanceBonus, 2, MidpointRounding.AwayFromZero);
    }
}

// Back-compat alias so existing callers keep compiling while the rename rolls through.
public static class ProfitService
{
    public static decimal Calculate(decimal totalPrice, double distanceKm)
        => DeliveryFeeCalculator.Calculate(totalPrice, distanceKm);
}
