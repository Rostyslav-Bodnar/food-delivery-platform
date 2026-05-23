using DF.Contracts.Enums;

namespace DF.OrderService.Application.Mappers;

public static class DeliveryMethodMapper
{
    public static Domain.Entities.DeliveryMethod ToDomain(this DeliveryMethod deliveryMethod)
        => (Domain.Entities.DeliveryMethod)deliveryMethod;

    public static DeliveryMethod ToContract(this Domain.Entities.DeliveryMethod deliveryMethod)
        => (DeliveryMethod)deliveryMethod;
}
