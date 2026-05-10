using DF.Contracts.Gateway.Requests.Dish;
using DF.Contracts.Gateway.Responses.Dish;
using DF.Contracts.RPC.Requests.UserService;
using DF.MenuService.Application.Mappers;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Exceptions;
using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Services;

public class DishService(
    IDishRepository repository,
    ICloudinaryService cloudinaryService,
    IIngredientService ingredientService,
    UserServiceRpcClient userServiceRpcClient)
    : IDishService
{
    public async Task<DishResponse> CreateDishAsync(CreateDishRequest request)
    {
        var accountResponse = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(request.BusinessId));

        if (accountResponse == null)
            throw new NullReferenceException("Account not found");

        if (!accountResponse.StripeChargesEnabled || !accountResponse.StripePayoutsEnabled)
            throw new StripeAccountNotReadyException();

        if (!string.IsNullOrWhiteSpace(accountResponse.StripeRequirementsDue))
            throw new StripeAccountNotReadyException("Stripe requirements are not completed.");

        string? imageUrl = null;

        if (request.Image != null)
        {
            var upload = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = upload.Url;
        }

        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description,
            Image = imageUrl,
            Price = request.Price,
            Category = request.Category.ToDomain(),
            BusinessId = request.BusinessId
        };

        var result = await repository.Create(dish);

        var ingredients = await ingredientService.CreateIngredients(
            request.Ingredients.Select(i =>
                new CreateIngredientRequest(i.Name, i.Weight)).ToList(),
            result.Id);

        return new DishResponse(
            result.Id,
            result.Name,
            result.Description,
            result.Image,
            result.Price,
            result.Category.ToContract(),
            result.CookingTime,
            ingredients
        );
    }

    public async Task<IEnumerable<DishResponse>> GetAllAsync()
    {
        var dishes = await repository.GetAll();

        var result = new List<DishResponse>();

        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            result.Add(new DishResponse(
                d.Id,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category.ToContract(),
                d.CookingTime,
                ingredients
            ));
        }

        return result;
    }

    public async Task<DishResponse?> GetByIdAsync(Guid id)
    {
        var d = await repository.Get(id);

        if (d == null)
            return null;

        var ingredients = await ingredientService.GetAllIngredientsByDishId(id);

        return new DishResponse(
            d.Id,
            d.Name,
            d.Description,
            d.Image,
            d.Price,
            d.Category.ToContract(),
            d.CookingTime,
            ingredients
        );
    }

    public async Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId)
    {
        var dishes = await repository.GetByBusinessIdAsync(businessId);

        var result = new List<DishResponse>();

        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            result.Add(new DishResponse(
                d.Id,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category.ToContract(),
                d.CookingTime,
                ingredients
            ));
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exists = await repository.Get(id);

        if (exists == null)
            throw new NullReferenceException($"Dish {id} not found");

        return await repository.Delete(id);
    }

    public async Task<DishResponse> UpdateDishAsync(UpdateDishRequest request)
    {
        var existing = await repository.Get(request.DishId);

        if (existing == null)
            throw new NullReferenceException($"Dish {request.DishId} not found");

        string? imageUrl = existing.Image;

        if (request.Image != null)
        {
            var upload = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = upload.Url;
        }

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.Category = request.Category.ToDomain();
        existing.Image = imageUrl;
        existing.CookingTime = request.CookingTime;

        await repository.Update(existing);

        var updatedIngredients =
            await ingredientService.UpdateIngredients(existing.Id, request.Ingredients);

        return new DishResponse(
            existing.Id,
            existing.Name,
            existing.Description,
            existing.Image,
            existing.Price,
            existing.Category.ToContract(),
            existing.CookingTime,
            updatedIngredients
        );
    }

    public async Task<DishForCustomerResponse> GetDishForCustomerAsync(Guid dishId)
    {
        var d = await repository.Get(dishId);

        if (d == null)
            throw new NullReferenceException($"Dish {dishId} not found");

        var ingredients = await ingredientService.GetAllIngredientsByDishId(dishId);

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(d.BusinessId));

        if (businessResponse == null)
            throw new NullReferenceException("Business not found");

        var businessDetails = new BusinessResponse(
            businessResponse.AccountId,
            businessResponse.Name,
            businessResponse.Description);

        return new DishForCustomerResponse(
            d.Id,
            d.Name,
            d.Description,
            d.Image,
            d.Price,
            d.Category.ToContract(),
            d.CookingTime,
            businessDetails,
            ingredients
        );
    }

    public async Task<List<DishForCustomerResponse>> GetAllDishForCustomerAsync()
    {
        var dishes = (await repository.GetAll()).ToList();

        var businessIds = dishes
            .Where(d => d.BusinessId != Guid.Empty)
            .Select(d => d.BusinessId)
            .Distinct()
            .ToList();

        var businessTasks = businessIds.ToDictionary(
            id => id,
            id => userServiceRpcClient.GetBusinessAccountAsync(
                new GetBusinessAccountRequest(id))
        );

        await Task.WhenAll(businessTasks.Values);

        var businesses = businessTasks.ToDictionary(
            x => x.Key,
            x =>
            {
                var result = x.Value.Result;

                if (result == null)
                    throw new NullReferenceException($"Business {x.Key} not found");

                return new BusinessResponse(
                    result.AccountId,
                    result.Name,
                    result.Description
                );
            }
        );

        var response = new List<DishForCustomerResponse>();

        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            var businessDetails = businesses.GetValueOrDefault(d.BusinessId);

            if (businessDetails == null)
                continue;

            response.Add(new DishForCustomerResponse(
                d.Id,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category.ToContract(),
                d.CookingTime,
                businessDetails,
                ingredients
            ));
        }

        return response;
    }

    public async Task<List<DishForCustomerResponse>> GetDishesForCustomerByBusinessIdAsync(Guid businessId)
    {
        var dishes = (await repository.GetByBusinessIdAsync(businessId)).ToList();

        if (!dishes.Any())
            return [];

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(businessId));

        if (businessResponse == null)
            throw new NullReferenceException("Business not found");

        var businessDetails = new BusinessResponse(
            businessResponse.AccountId,
            businessResponse.Name,
            businessResponse.Description);

        var result = new List<DishForCustomerResponse>();

        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            result.Add(new DishForCustomerResponse(
                d.Id,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category.ToContract(),
                d.CookingTime,
                businessDetails,
                ingredients
            ));
        }

        return result;
    }
}