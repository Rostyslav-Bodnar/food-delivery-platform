namespace DF.MenuService.Contracts.Models.Response;

public record IngredientResponse(Guid Id, Guid DishId,  string Name, int Weight);