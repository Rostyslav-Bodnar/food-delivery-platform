namespace DF.OrderService.Contracts.Models.Responses;

/// <summary>
/// Revenue contribution of a single dish for a business inside a date
/// window. Aggregated from delivered orders only. Surfaced to the
/// business dashboard's per-dish bar chart.
/// </summary>
public sealed record DishRevenueResponse(
    Guid DishId,
    string DishName,
    int QuantitySold,
    int OrderCount,
    decimal Revenue
);
