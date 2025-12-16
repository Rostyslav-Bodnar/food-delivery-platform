using DF.OrderService.Contracts.Models.Requests;
using DF.OrderService.Contracts.Models.Responses;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Services.Interfaces;

public interface IOrderService
{
    Task<bool> CreateOrdersAsync(List<CreateOrderRequest> orderRequests);
    Task<bool>  CreateOrderAsync(CreateOrderRequest request);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync();
    Task<IEnumerable<BusinessOrderResponse>> GetAllByBusinessIdAsync(Guid businessId);
    Task<IEnumerable<CustomerOrderResponse>> GetAllByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<CourierOrderResponse>> GetAllByCourierIdAsync(Guid courierId);

    Task<IEnumerable<CustomerOrderResponse>> GetCustomerOrderHistoryAsync(Guid customerId);
    Task<IEnumerable<CourierOrderResponse>> GetCourierOrderHistoryAsync(Guid courierId);
    
    Task<OrderResponse> ChangeOrderStatus(Guid orderId,  OrderStatus status);
    Task<OrderResponse> DeliverOrderAsync(Guid orderId, Guid courierId);
}