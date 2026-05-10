using System;
using System.Collections.Generic;
using DF.Contracts.Enums;

namespace DF.Contracts.Gateway.Responses.Dish;

public record DishResponse(
    Guid Id, 
    string Name, 
    string? Description, 
    string? ImageUrl, 
    decimal Price, 
    Category Category,
    int CookingTime,
    List<IngredientResponse> Ingredients);
    
public record DishForCustomerResponse(
    Guid Id, 
    string Name, 
    string? Description, 
    string? ImageUrl, 
    decimal Price, 
    Category Category,
    int CookingTime,
    BusinessResponse BusinessDetails,
    List<IngredientResponse> Ingredients);