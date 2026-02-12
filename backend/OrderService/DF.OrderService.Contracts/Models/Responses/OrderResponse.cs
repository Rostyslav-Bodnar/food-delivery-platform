using DF.Contracts.RPC.Responses.UserService;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Contracts.Models.Responses;

public record OrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice
    );

public record CustomerOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    string BusinessAddress,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    Guid DeliveredBy,
    string CourierName,
    string OrderStatus,
    List<DishResponse> dishes
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice);

public record BusinessOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    string CustomerFullName,
    string CustomerAddress,
    DateTime OrderDate,
    decimal TotalPrice,
    Guid DeliveredBy,
    string CourierName,
    string OrderStatus,
    List<DishResponse> dishes
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice);

public record CourierOrderResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid OrderedBy,
    string CustomerFullName,
    string CustomerAddress,
    string CustomerPhoneNumber,
    DateTime OrderDate,
    decimal TotalPrice,
    string OrderStatus,
    decimal Profit
) : OrderResponse(Id, BusinessId, BusinessName, OrderedBy, OrderDate, TotalPrice);

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