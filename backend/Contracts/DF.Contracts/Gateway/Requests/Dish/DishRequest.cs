using System;
using System.Collections.Generic;
using DF.Contracts.Enums;
using Microsoft.AspNetCore.Http;

namespace DF.Contracts.Gateway.Requests.Dish;

public record CreateDishRequest(
    Guid BusinessId,
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    IFormFile? Image,
    int CookingTime,
    List<CreateIngredientRequest> Ingredients);
    
public record UpdateDishRequest(
    Guid DishId,
    string Name, 
    string? Description, 
    decimal Price, 
    Category Category, 
    int CookingTime,
    IFormFile? Image,
    List<UpdateIngredientRequest> Ingredients);