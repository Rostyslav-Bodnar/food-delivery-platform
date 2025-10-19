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
    
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDish([FromForm] CreateDishRequest request)
    {
        var result = await dishService.CreateDishAsync(request);
        return Ok(result);
    }
}