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
    
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDish([FromForm] CreateDishRequest request)
    {
        var result = await dishService.CreateDishAsync(request);
        return Ok(result);
    }
}