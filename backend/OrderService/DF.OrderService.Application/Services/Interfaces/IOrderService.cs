using DF.Contracts.Enums;
using DF.Contracts.Gateway.Requests.Order;
using DF.Contracts.Gateway.Responses.Order;
using DF.OrderService.Contracts.Pagination;
using DishRevenueResponse = DF.OrderService.Contracts.Models.Responses.DishRevenueResponse;

namespace DF.OrderService.Application.Services.Interfaces;

public interface IOrderService
{
    Task<bool> CreateOrdersAsync(List<CreateOrderRequest> orderRequests);
    Task<bool>  CreateOrderAsync(CreateOrderRequest request);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync();
    Task<PagedResponse<OrderResponse>> GetAllOrdersPagedAsync(PageRequest page);
    Task<OrderDetailsResponse> GetOrderAsync(Guid orderId);
    Task<IEnumerable<BusinessOrderResponse>> GetAllByBusinessIdAsync(Guid businessId);
    Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId);
    Task<IEnumerable<CourierOrderResponse>> GetActiveByCourierIdAsync(Guid courierId);
    Task<IEnumerable<CustomerOrderResponse>> GetCustomerOrderHistoryAsync(Guid customerId);
    Task<IEnumerable<BusinessOrderResponse>> GetBusinessOrderHistoryAsync(Guid businessId);
    Task<IEnumerable<CourierOrderResponse>> GetCourierOrderHistoryAsync(Guid courierId);
    Task<OrderResponse> ChangeOrderStatus(Guid orderId,  OrderStatus status);
    Task<OrderResponse> DeliverOrderAsync(Guid orderId, Guid courierId);

    Task<bool> CancelOrderAsync(Guid orderId);

    Task<OrderResponse> MarkCourierPaidAsync(Guid orderId, Guid courierId);

    /// <summary>
    /// Per-dish revenue for a business in [fromUtc, toUtc] across delivered orders.
    /// </summary>
    Task<IReadOnlyList<DishRevenueResponse>> GetRevenueByDishAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
}
