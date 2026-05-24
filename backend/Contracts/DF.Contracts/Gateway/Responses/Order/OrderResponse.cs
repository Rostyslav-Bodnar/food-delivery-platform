using System;
using System.Collections.Generic;

namespace DF.Contracts.Gateway.Responses.Order;

public record OrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal CourierFee,
    bool CourierPaid,
    bool IsPaid = false
    );

public record CustomerOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    LocationResponse BusinessLocation,
    LocationResponse CustomerLocation,
    LocationResponse CourierLocation,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal CourierFee,
    bool CourierPaid,
    Guid DeliveredBy,
    string CourierName,
    string OrderStatus,
    List<DishResponse> dishes,
    string DeliveryMethod = "",
    string PaymentMethod = "",
    bool IsPaid = false
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice, DeliveryFee, CourierFee, CourierPaid, IsPaid);

public record BusinessOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    LocationResponse BusinessLocation,
    LocationResponse CustomerLocation,
    LocationResponse CourierLocation,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal CourierFee,
    bool CourierPaid,
    Guid DeliveredBy,
    string CourierName,
    string OrderStatus,
    List<DishResponse> dishes,
    string DeliveryMethod = "",
    string PaymentMethod = "",
    bool IsPaid = false
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice, DeliveryFee, CourierFee, CourierPaid, IsPaid);

public record CourierOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    LocationResponse BusinessLocation,
    LocationResponse CustomerLocation,
    LocationResponse CourierLocation,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal CourierFee,
    bool CourierPaid,
    string OrderStatus,
    decimal Profit,
    string PaymentMethod = ""
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice, DeliveryFee, CourierFee, CourierPaid);

public record OrderDetailsResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedById,
    string CustomerFullName,
    string CustomerAddress,
    string CustomerPhoneNumber,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal DeliveryFee,
    decimal CourierFee,
    bool CourierPaid,
    string OrderStatus,
    decimal Profit,
    List<DishResponse> dishes,
    Guid? DeliveredById,
    string? CourierName,
    string? CourierPhoneNumber,
    string DeliveryMethod,
    string PaymentMethod = "",
    bool IsPaid = false
    );
    
    public record DishResponse(
        Guid Id,
        Guid BusinessId,
        string DishName,
        int  Quantity,
        decimal Price
        );