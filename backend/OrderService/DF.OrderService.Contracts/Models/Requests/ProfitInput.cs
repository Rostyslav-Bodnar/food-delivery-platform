namespace DF.OrderService.Contracts.Models.Requests;

public record ProfitInput(
    decimal OrderTotal,
    double DistanceMeters,
    double DurationSeconds,
    decimal SharePercent,       // e.g. 0.25m
    decimal CostPerKm,          // e.g. 3.5m
    decimal CostPerMinute,      // e.g. 0.5m
    decimal SlaBonus,           // e.g. 10m
    decimal LatePenalty,        // e.g. 5m
    decimal DeviationPenalty    // e.g. 0m..X
);