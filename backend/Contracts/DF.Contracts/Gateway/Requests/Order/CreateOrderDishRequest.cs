using System;

namespace DF.Contracts.Gateway.Requests.Order;

public record CreateOrderDishRequest(
    Guid OrderId,
    Guid DishId,
    int Quantity = 1
);
