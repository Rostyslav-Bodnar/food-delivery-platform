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
    bool CourierPaid
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
    List<DishResponse> dishes
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice, DeliveryFee, CourierFee, CourierPaid);

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
    List<DishResponse> dishes
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice, DeliveryFee, CourierFee, CourierPaid);

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
    decimal Profit
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
    string? CourierPhoneNumber
    );
    
    public record DishResponse(
        Guid Id,
        Guid BusinessId,
        string DishName,
        int  Quantity,
        decimal Price
        );