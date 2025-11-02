using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Contracts.Models.Response;

public record DishResponse(
    Guid Id, 
    Guid? MenuId, 
    string Name, 
    string? Description, 
    string? ImageUrl, 
    decimal Price, 
    Category Category);