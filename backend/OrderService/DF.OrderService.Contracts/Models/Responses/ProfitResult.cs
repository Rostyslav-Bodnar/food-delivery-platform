namespace DF.OrderService.Contracts.Models.Responses;

public record ProfitResult(
    decimal Profit, 
    decimal BaseShare, 
    decimal DistanceCost, 
    decimal TimeCost, 
    decimal Bonuses, 
    decimal Penalties
    );
