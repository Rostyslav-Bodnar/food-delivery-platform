using DF.Contracts.Gateway.Requests.Dish;
using DF.MenuService.API.Filters;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Exceptions;
using DF.MenuService.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace DF.MenuService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishController(IDishService dishService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var result = await dishService.GetPagedAsync(PageRequest.From(page, pageSize));
        // Pagination still runs server-side; expose meta via headers so the wire shape
        // stays a flat array (what the gateway + frontend currently deserialize).
        AppendPaginationHeaders(result.Page, result.PageSize, result.TotalCount, result.TotalPages);
        return Ok(result.Items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var dish = await dishService.GetByIdAsync(id);

        if (dish == null)
            throw new NotFoundException("Dish not found");

        return Ok(dish);
    }

    [HttpGet("customer/{id:guid}")]
    public async Task<IActionResult> GetForCustomer(Guid id)
    {
        var result = await dishService.GetDishForCustomerAsync(id);
        
        if (result == null)
            throw new NotFoundException("Dish not found");

        return Ok(result);
    }

    [HttpGet("customer")]
    public async Task<IActionResult> GetAllForCustomer([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var result = await dishService.GetAllForCustomerPagedAsync(PageRequest.From(page, pageSize));
        AppendPaginationHeaders(result.Page, result.PageSize, result.TotalCount, result.TotalPages);
        return Ok(result.Items);
    }

    [HttpGet("customer/{businessId:guid}/dish")]
    public async Task<IActionResult> GetByBusinessIdForCustomer(Guid businessId, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var result = await dishService.GetForCustomerByBusinessPagedAsync(businessId, PageRequest.From(page, pageSize));
        AppendPaginationHeaders(result.Page, result.PageSize, result.TotalCount, result.TotalPages);
        return Ok(result.Items);
    }

    private void AppendPaginationHeaders(int page, int pageSize, int totalCount, int totalPages)
    {
        Response.Headers["X-Pagination-Page"] = page.ToString();
        Response.Headers["X-Pagination-PageSize"] = pageSize.ToString();
        Response.Headers["X-Pagination-TotalCount"] = totalCount.ToString();
        Response.Headers["X-Pagination-TotalPages"] = totalPages.ToString();
    }

    [HttpGet("{businessId:guid}/dish")]
    public async Task<IActionResult> GetByBusinessId(Guid businessId)
    {
        var result =  await dishService.GetByBusinessId(businessId);
        
        if (result == null)
            throw new NotFoundException("Dish not found");

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDish(Guid id)
    {
        var result = await dishService.DeleteAsync(id);
        return Ok(result);
    }
    
    [HttpPost("create")]
    [Idempotent]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDish([FromForm] CreateDishRequest request)
    {
        var result = await dishService.CreateDishAsync(request);
        return Ok(result);
    }

    [HttpPost("update")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateDish([FromForm] UpdateDishRequest request)
    {
        var result = await dishService.UpdateDishAsync(request);
        return Ok(result);
    }
}