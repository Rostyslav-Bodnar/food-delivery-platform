using DF.Contracts.Gateway.Requests.Dish;
using DF.Contracts.Gateway.Responses.Dish;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IIngredientService
{
    Task<IngredientResponse> CreateIngredient(CreateIngredientRequest req, Guid dishId);
    Task<IngredientResponse> UpdateIngredient(UpdateIngredientRequest req);
    Task<List<IngredientResponse>> GetAllIngredients();
    Task<List<IngredientResponse>> GetAllIngredientsByDishId(Guid dishId);
    Task<List<IngredientResponse>> CreateIngredients(IEnumerable<CreateIngredientRequest> req, Guid dishId);
    Task<List<IngredientResponse>> UpdateIngredients(Guid dishId, List<UpdateIngredientRequest> ingredients);
}