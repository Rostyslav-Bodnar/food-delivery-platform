using System;
using System.Collections.Generic;
using DF.Contracts.Enums;

namespace DF.Contracts.Gateway.Requests.Order;

public record CreateOrderRequest(
    Guid BusinessId,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    Guid? DeliveredBy,
    CreateLocationRequest DeliverTo,
    CreateLocationRequest DeliverFrom,
    PaymentMethod PaymentMethod,
    List<CreateOrderDishRequest> Dishes
);