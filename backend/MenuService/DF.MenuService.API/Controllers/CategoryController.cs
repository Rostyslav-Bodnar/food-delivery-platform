using DF.MenuService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DF.MenuService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = Enum.GetValues(typeof(Category)).Cast<Category>();
        return Ok(categories);
    }
}