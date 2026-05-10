using DF.Contracts.Gateway.Responses;
using DF.Contracts.Gateway.Responses.Dish;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.MenuService;

[Authorize]
[ApiController]
[Route("api/dish")]
[GatewayService(ServiceType.MenuService)]
public class DishController(GatewayProxy proxy)
    : ControllerBase
{
    // =========================
    // GET ALL DISHES
    // =========================

    [HttpGet]
    public Task<Response<IEnumerable<DishResponse>>> GetAll()
        => proxy.ProxyAsync<IEnumerable<DishResponse>>(HttpContext);

    // =========================
    // GET DISH BY ID
    // =========================

    [HttpGet("{id:guid}")]
    public Task<Response<DishResponse>> Get(Guid id)
        => proxy.ProxyAsync<DishResponse>(HttpContext);

    // =========================
    // GET ALL DISHES FOR CUSTOMER
    // =========================

    [HttpGet("customer")]
    [AllowAnonymous]
    public Task<Response<IEnumerable<DishForCustomerResponse>>> GetAllForCustomer()
        => proxy.ProxyAsync<IEnumerable<DishForCustomerResponse>>(HttpContext);

    // =========================
    // GET DISH FOR CUSTOMER
    // =========================

    [HttpGet("customer/{id:guid}")]
    [AllowAnonymous]
    public Task<Response<DishForCustomerResponse>> GetForCustomer(Guid id)
        => proxy.ProxyAsync<DishForCustomerResponse>(HttpContext);

    // =========================
    // GET DISHES BY BUSINESS ID
    // FOR CUSTOMER
    // =========================

    [HttpGet("customer/{businessId:guid}/dish")]
    [AllowAnonymous]
    public Task<Response<IEnumerable<DishForCustomerResponse>>> GetByBusinessIdForCustomer(
        Guid businessId
    )
        => proxy.ProxyAsync<IEnumerable<DishForCustomerResponse>>(HttpContext);

    // =========================
    // GET DISHES BY BUSINESS ID
    // =========================

    [HttpGet("{businessId:guid}/dish")]
    public Task<Response<IEnumerable<DishResponse>>> GetByBusinessId(
        Guid businessId
    )
        => proxy.ProxyAsync<IEnumerable<DishResponse>>(HttpContext);

    // =========================
    // CREATE DISH
    // =========================

    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    public Task<Response<DishResponse>> Create()
        => proxy.ProxyAsync<DishResponse>(HttpContext);

    // =========================
    // UPDATE DISH
    // =========================

    [HttpPost("update")]
    [Consumes("multipart/form-data")]
    public Task<Response<DishResponse>> Update()
        => proxy.ProxyAsync<DishResponse>(HttpContext);

    // =========================
    // DELETE DISH
    // =========================

    [HttpDelete("{id:guid}")]
    public Task<Response<bool>> Delete(Guid id)
        => proxy.ProxyAsync<bool>(HttpContext);
}