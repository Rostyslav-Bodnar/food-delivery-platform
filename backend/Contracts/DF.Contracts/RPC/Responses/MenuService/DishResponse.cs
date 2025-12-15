using DF.Contracts.RPC.Requests.MenuService;

namespace DF.Contracts.RPC.Responses.MenuService;

public record GetDishesResponse(
    List<GetDishResponse> Dishes
    );

public record GetDishResponse(
    Guid DishId,
    string Name,
    string Description,
    string Image,
    decimal Price,
    int CategoryId,
    string CategoryName,
    int CookingTime,
    Guid BusinessId,
    GetIngredientsResponse Ingredients
    );
    
public record GetIngredientsResponse(
    List<GetIngredientResponse> Ingredients
    );

public record GetIngredientResponse(
    Guid IngredientId,
    Guid DishId,
    string Name,
    int Weight
    );