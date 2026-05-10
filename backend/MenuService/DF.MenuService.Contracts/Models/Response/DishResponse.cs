using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Contracts.Models.Response;

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