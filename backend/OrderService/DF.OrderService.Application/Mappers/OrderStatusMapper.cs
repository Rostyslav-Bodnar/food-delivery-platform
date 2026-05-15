using DF.Contracts.Enums;

namespace DF.OrderService.Application.Mappers;

public static class OrderStatusMapper
{
    public static Domain.Entities.OrderStatus ToDomain(this OrderStatus paymentMethod)
        => (Domain.Entities.OrderStatus)paymentMethod;

    public static OrderStatus ToContract(this Domain.Entities.OrderStatus paymentMethod)
        => (OrderStatus)paymentMethod;
}
