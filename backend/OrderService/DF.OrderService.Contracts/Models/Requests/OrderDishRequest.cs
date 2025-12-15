namespace DF.OrderService.Contracts.Models.Requests;

public record CreateOrderDishRequest(
    Guid OrderId,
    Guid DishId
    );