using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Contracts.Models.Requests;

public record CreateOrderRequest(
    Guid BusinessId,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    Guid? DeliveredBy,
    OrderStatus OrderStatus,
    CreateLocationRequest DeliverTo,
    CreateLocationRequest DeliverFrom,
    List<CreateOrderDishRequest> Dishes,
    string OrderNumber
);

    