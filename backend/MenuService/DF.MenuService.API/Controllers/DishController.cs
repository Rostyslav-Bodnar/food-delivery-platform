using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace DF.MenuService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishController(IDishService dishService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var dishes = await dishService.GetAllAsync();
        return Ok(dishes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var dish = await dishService.GetByIdAsync(id);

        if (dish == null)
            return NotFound();

        return Ok(dish);
    }

    [HttpGet("customer/{id:guid}")]
    public async Task<IActionResult> GetForCustomer(Guid id)
    {
        var result = await dishService.GetDishForCustomerAsync(id);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("customer")]
    public async Task<IActionResult> GetAllForCustomer()
    {
        var dishes = await dishService.GetAllDishForCustomerAsync();
        return Ok(dishes);
    }

    [HttpGet("customer/{businessId:guid}/dish")]
    public async Task<IActionResult> GetByBusinessIdForCustomer(Guid businessId)
    {
        var result =  await dishService.GetDishesForCustomerByBusinessIdAsync(businessId);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{businessId:guid}/dish")]
    public async Task<IActionResult> GetByBusinessId(Guid businessId)
    {
        var result =  await dishService.GetByBusinessId(businessId);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDish(Guid id)
    {
        var result = await dishService.DeleteAsync(id);
        return Ok(result);
    }
    
    [HttpPost("create")]
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