using DF.Contracts.Enums;

namespace DF.OrderService.Application.Mappers;

public static class PaymentMethodMapper
{
    public static Domain.Entities.PaymentMethod ToDomain(this PaymentMethod paymentMethod)
        => (Domain.Entities.PaymentMethod)paymentMethod;

    public static PaymentMethod ToContract(this Domain.Entities.PaymentMethod paymentMethod)
        => (PaymentMethod)paymentMethod;
}
