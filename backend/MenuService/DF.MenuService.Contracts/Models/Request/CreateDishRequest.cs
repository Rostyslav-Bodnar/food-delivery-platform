using DF.MenuService.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Contracts.Models.Request;

public record CreateDishRequest(
    Guid? MenuId, 
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    IFormFile? Image);
    
public record UpdateDishRequest(
    Guid DishId,
    Guid? MenuId, 
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    IFormFile? Image);