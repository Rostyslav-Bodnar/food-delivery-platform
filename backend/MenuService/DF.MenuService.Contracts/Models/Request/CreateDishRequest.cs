using DF.MenuService.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Contracts.Models.Request;

public record CreateDishRequest(
    Guid UserId,
    Guid? MenuId, 
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    IFormFile? Image,
    int CookingTime,
    List<CreateIngredientRequest> Ingredients);
    
public record UpdateDishRequest(
    Guid DishId,
    Guid? MenuId, 
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    int CookingTime,
    IFormFile? Image);