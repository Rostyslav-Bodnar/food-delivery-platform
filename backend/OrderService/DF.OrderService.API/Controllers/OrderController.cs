using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Models.Requests;
using DF.OrderService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DF.OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await orderService.GetAllOrdersAsync());
    }

    [HttpGet("business")]
    public async Task<IActionResult> GetByBusiness(Guid businessId)
    {
        return Ok(await orderService.GetAllByBusinessIdAsync(businessId));
    }

    [HttpGet("courier")]
    public async Task<IActionResult> GetByCourier(Guid courierId)
    {
        return Ok(await orderService.GetAllByCourierIdAsync(courierId));
    }

    [HttpPatch("status")]
    public async Task<IActionResult> ChangeStatus(Guid orderId, OrderStatus status)
    {
        return Ok(await orderService.ChangeOrderStatus(orderId, status));
    }

    [HttpGet]
    [Route("get-order-details/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(Guid orderId)
    {
        var result = await orderService.GetOrderAsync(orderId);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("get-customer-orders")]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId)
    {
        var result = await orderService.GetAllByCustomerIdAsync(customerId);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("get-customer-history")]
    public async Task<IActionResult> GetCustomerOrderHistory(Guid customerId)
    {
        var result = await orderService.GetCustomerOrderHistoryAsync(customerId);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("get-courier-history")]
    public async Task<IActionResult> GetCourierOrderHistory(Guid customerId)
    {
        var result = await orderService.GetCourierOrderHistoryAsync(customerId);
        return Ok(result);
    }
    
    [HttpPost]
    [Route("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await orderService.CreateOrderAsync(request);
        return Ok(result);
    }
    
    [HttpPost]
    [Route("create-orders")]
    public async Task<IActionResult> CreateOrders([FromBody] List<CreateOrderRequest> request)
    {
        var result = await orderService.CreateOrdersAsync(request);
        return Ok(result);
    }

    [HttpPost]
    [Route("courier/deliver")]
    public async Task<IActionResult> DeliverOrder(Guid orderId, Guid courierId)
    {
        var result = await orderService.DeliverOrderAsync(orderId, courierId);
        return Ok(result);
    }
}