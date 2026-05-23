using DF.Contracts.Enums;
using DF.Contracts.Gateway.Requests.Order;
using DF.OrderService.API.Filters;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace DF.OrderService.API.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        => Ok(await orderService.GetAllOrdersPagedAsync(PageRequest.From(page, pageSize)));

    [HttpGet("business")]
    public async Task<IActionResult> GetByBusiness([FromQuery] Guid businessId)
        => Ok(await orderService.GetAllByBusinessIdAsync(businessId));

    [HttpGet("courier")]
    public async Task<IActionResult> GetByCourier([FromQuery] Guid courierId)
        => Ok(await orderService.GetAllByCourierIdAsync(courierId));

    [HttpGet("courier/active")]
    public async Task<IActionResult> GetActiveByCourier([FromQuery] Guid courierId)
        => Ok(await orderService.GetActiveByCourierIdAsync(courierId));

    [HttpPatch("status")]
    public async Task<IActionResult> ChangeStatus([FromQuery] Guid orderId, [FromQuery] OrderStatus status)
        => Ok(await orderService.ChangeOrderStatus(orderId, status));

    [HttpPatch("cancel")]
    public async Task<IActionResult> CancelOrder([FromQuery] Guid orderId)
        => Ok(await orderService.CancelOrderAsync(orderId));

    [HttpGet("details/{orderId:guid}")]
    public async Task<IActionResult> GetOrderDetails(Guid orderId)
        => Ok(await orderService.GetOrderAsync(orderId));

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId)
        => Ok(await orderService.GetAllByCustomerIdAsync(customerId));

    [HttpGet("customer/{customerId}/history")]
    public async Task<IActionResult> GetCustomerOrderHistory(Guid customerId)
        => Ok(await orderService.GetCustomerOrderHistoryAsync(customerId));

    [HttpGet("courier/{courierId}/history")]
    public async Task<IActionResult> GetCourierOrderHistory(Guid courierId)
        => Ok(await orderService.GetCourierOrderHistoryAsync(courierId));

    [HttpPost("create")]
    [Idempotent]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        => Ok(await orderService.CreateOrderAsync(request));

    [HttpPost("create/batch")]
    public async Task<IActionResult> CreateOrders([FromBody] List<CreateOrderRequest> request)
        => Ok(await orderService.CreateOrdersAsync(request));

    [HttpPost("courier/deliver")]
    public async Task<IActionResult> DeliverOrder([FromQuery] Guid orderId, [FromQuery] Guid courierId)
        => Ok(await orderService.DeliverOrderAsync(orderId, courierId));

    [HttpPatch("courier/mark-paid")]
    public async Task<IActionResult> MarkCourierPaid([FromQuery] Guid orderId, [FromQuery] Guid courierId)
        => Ok(await orderService.MarkCourierPaidAsync(orderId, courierId));
}
