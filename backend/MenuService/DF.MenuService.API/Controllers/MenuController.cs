using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace DF.MenuService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var menus = await menuService.GetAllAsync();
        return Ok(menus);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var menu = await menuService.GetAsync(id);
        if (menu == null) return NotFound();
        return Ok(menu);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateMenuRequest request)
    {
        var created = await menuService.CreateAsync(request, request.ImageFile);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

}