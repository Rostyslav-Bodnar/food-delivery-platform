using DF.Contracts.Enums;
using DF.Contracts.Gateway.Requests.Order;
using DF.Contracts.Gateway.Responses;
using DF.Contracts.Gateway.Responses.Order;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.OrderService;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[GatewayService(ServiceType.OrderService)]
public class OrderController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET ALL
    // =========================
    [HttpGet("all")]
    public Task<Response<IEnumerable<OrderResponse>>> GetAll()
        => proxy.ProxyAsync<IEnumerable<OrderResponse>>(HttpContext);

    // =========================
    // GET DETAILS
    // =========================
    [HttpGet("details/{orderId:guid}")]
    public Task<Response<OrderDetailsResponse>> GetOrderDetails(Guid orderId)
        => proxy.ProxyAsync<OrderDetailsResponse>(HttpContext);

    // =========================
    // GET BY BUSINESS
    // =========================
    [HttpGet("business")]
    public Task<Response<IEnumerable<BusinessOrderResponse>>> GetByBusiness(
        [FromQuery] Guid businessId)
        => proxy.ProxyAsync<IEnumerable<BusinessOrderResponse>>(HttpContext);

    // =========================
    // GET BY COURIER
    // =========================
    [HttpGet("courier")]
    public Task<Response<IEnumerable<CourierOrderResponse>>> GetByCourier(
        [FromQuery] Guid courierId)
        => proxy.ProxyAsync<IEnumerable<CourierOrderResponse>>(HttpContext);

    // =========================
    // ACTIVE COURIER ORDERS
    // =========================
    [HttpGet("courier/active")]
    public Task<Response<IEnumerable<CourierOrderResponse>>> GetActiveByCourier(
        [FromQuery] Guid courierId)
        => proxy.ProxyAsync<IEnumerable<CourierOrderResponse>>(HttpContext);

    // =========================
    // CHANGE STATUS
    // =========================
    [HttpPatch("status")]
    public Task<Response<OrderResponse>> ChangeStatus(
        [FromQuery] Guid orderId,
        [FromQuery] OrderStatus status)
        => proxy.ProxyAsync<OrderResponse>(HttpContext);

    // =========================
    // CANCEL ORDER
    // =========================
    [HttpPatch("cancel")]
    public Task<Response<bool>> CancelOrder(
        [FromQuery] Guid orderId)
        => proxy.ProxyAsync<bool>(HttpContext);

    // =========================
    // GET CUSTOMER ORDERS
    // =========================
    [HttpGet("customer/{customerId}")]
    public Task<Response<IEnumerable<CustomerOrderResponse>>> GetByCustomer(Guid customerId)
        => proxy.ProxyAsync<IEnumerable<CustomerOrderResponse>>(HttpContext);

    // =========================
    // CUSTOMER HISTORY
    // =========================
    [HttpGet("customer/{customerId}/history")]
    public Task<Response<IEnumerable<CustomerOrderResponse>>> GetCustomerHistory(Guid customerId)
        => proxy.ProxyAsync<IEnumerable<CustomerOrderResponse>>(HttpContext);

    // =========================
    // COURIER HISTORY
    // =========================
    [HttpGet("courier/{courierId}/history")]
    public Task<Response<IEnumerable<CourierOrderResponse>>> GetCourierHistory(Guid courierId)
        => proxy.ProxyAsync<IEnumerable<CourierOrderResponse>>(HttpContext);

    // =========================
    // CREATE ORDER
    // =========================
    [HttpPost("create")]
    public Task<Response<bool>> CreateOrder(
        [FromBody] CreateOrderRequest request)
        => proxy.ProxyAsync<bool>(HttpContext);

    // =========================
    // CREATE BATCH
    // =========================
    [HttpPost("create/batch")]
    public Task<Response<bool>> CreateOrders()
        => proxy.ProxyAsync<bool>(HttpContext);

    // =========================
    // DELIVER ORDER
    // =========================
    [HttpPost("courier/deliver")]
    public Task<Response<OrderResponse>> DeliverOrder(
        [FromQuery] Guid orderId,
        [FromQuery] Guid courierId)
        => proxy.ProxyAsync<OrderResponse>(HttpContext);
}