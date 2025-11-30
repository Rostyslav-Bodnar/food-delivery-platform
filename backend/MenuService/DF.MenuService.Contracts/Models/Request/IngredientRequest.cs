namespace DF.MenuService.Contracts.Models.Request;

public record CreateIngredientRequest(string Name, int Weight);

public record UpdateIngredientRequest(Guid? Id, string Name, int Weight);