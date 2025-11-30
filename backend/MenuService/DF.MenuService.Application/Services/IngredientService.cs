using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;
using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Services;

public class IngredientService(IIngredientRepository repository) : IIngredientService
{
    public async Task<IngredientResponse> CreateIngredient(CreateIngredientRequest req, Guid dishId)
    {
        var entity = new Ingredient
        {
            DishId = dishId,
            Name = req.Name,
            Weight = req.Weight
        };
        
        var result = await repository.Create(entity);
        return new IngredientResponse(result.Id, result.DishId,  result.Name, result.Weight);
    }

    public async Task<List<IngredientResponse>> CreateIngredients(IEnumerable<CreateIngredientRequest> req, Guid dishId)
    {
        var entities = req.Select(r => new Ingredient
        {
            DishId = dishId,
            Name = r.Name,
            Weight = r.Weight
        }).ToList();

        var created = await repository.CreateIngredients(entities);

        return created
            .Select(i => new IngredientResponse(
                i.Id,
                i.DishId,
                i.Name,
                i.Weight
            ))
            .ToList();
    }

 
    public async Task<IngredientResponse> UpdateIngredient(UpdateIngredientRequest req)
    {
        var entity = await repository.Get(req.Id);
        if(entity == null)
            throw new NullReferenceException($"Ingredient with id {req.Id} not found");
        
        entity.Name = req.Name;
        entity.Weight = req.Weight;
        var result = await repository.Update(entity);
        return new IngredientResponse(result.Id, result.DishId, result.Name, result.Weight);
    }

    public async Task<List<IngredientResponse>> GetAllIngredients()
    {
        var ingredients = await repository.GetAll();

        return ingredients
            .Select(i => new IngredientResponse(
                i.Id,
                i.DishId,
                i.Name,
                i.Weight
            ))
            .ToList();
    }

    public async Task<List<IngredientResponse>> GetAllIngredientsByDishId(Guid dishId)
    {
        var ingredients = await repository.GetAllIngredientsByDishId(dishId);
        
        return ingredients
            .Select(i => new IngredientResponse(
                i.Id,
                i.DishId,
                i.Name,
                i.Weight
            ))
            .ToList();
    }
}