using DF.Contracts.RPC.Requests.UserService;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;
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
        // 1. Отримати бізнес-акаунт
        var accountResponse = await userServiceRpcClient.GetAccountAsync(
            new GetAccountRequest(request.UserId));

        // 2. Завантажити картинку
        string? imageUrl = null;
        if (request.Image != null)
        {
            var upload = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = upload.Url;
        }

        // 3. Створити dish
        var dish = new Dish
        {
            MenuId = request.MenuId,
            Name = request.Name,
            Description = request.Description,
            Image = imageUrl,
            Price = request.Price,
            Category = request.Category,
            BusinessId = accountResponse.AccountId
        };

        // 4. Зберегти dish
        var result = await repository.Create(dish);

        // 5. Проставити DishId для інгредієнтів
        var ingredientsWithDishId = request.Ingredients
            .Select(i => new CreateIngredientRequest(
                Name: i.Name,
                Weight: i.Weight
            )).ToList();

        // 6. Створити інгредієнти
        var ingredients = await ingredientService.CreateIngredients(ingredientsWithDishId, result.Id);

        // 7. Повернути DTO
        return new DishResponse(
            dish.Id,
            dish.MenuId,
            dish.Name,
            dish.Description,
            dish.Image,
            dish.Price,
            dish.Category,
            dish.CookingTime,
            ingredients
        );
    }

    public async Task<IEnumerable<DishResponse>> GetAllAsync()
    {
        var dishes = await repository.GetAll();

        // Потрібно підтягувати інгредієнти
        var result = new List<DishResponse>();
        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            result.Add(new DishResponse(
                d.Id,
                d.MenuId,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category,
                d.CookingTime,
                ingredients
            ));
        }

        return result;
    }

    public async Task<DishResponse> GetByIdAsync(Guid id)
    {
        var d = await repository.Get(id);
        var ingredients = await ingredientService.GetAllIngredientsByDishId(id);

        return new DishResponse(
            d.Id,
            d.MenuId,
            d.Name,
            d.Description,
            d.Image,
            d.Price,
            d.Category,
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
                d.MenuId,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category,
                d.CookingTime,
                ingredients
            ));
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
        => await repository.Delete(id);

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
        existing.Category = request.Category;
        existing.Image = imageUrl;
        existing.CookingTime = request.CookingTime;

        await repository.Update(existing);
        
        var updatedIngredients = await ingredientService.UpdateIngredients(existing.Id, request.Ingredients);

        return new DishResponse(
            existing.Id,
            existing.MenuId,
            existing.Name,
            existing.Description,
            existing.Image,
            existing.Price,
            existing.Category,
            existing.CookingTime,
            updatedIngredients
        );
    }

    public async Task<DishForCustomerResponse> GetDishForCustomerAsync(Guid dishId)
    {
        var d = await repository.Get(dishId);
        var ingredients = await ingredientService.GetAllIngredientsByDishId(dishId);

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(d.BusinessId));

        var businessDetails = new BusinessResponse(businessResponse.AccountId,  businessResponse.Name, businessResponse.Description);
        
        return new DishForCustomerResponse(
            d.Id,
            d.MenuId,
            d.Name,
            d.Description,
            d.Image,
            d.Price,
            d.Category,
            d.CookingTime,
            businessDetails,
            ingredients
        );
    }

    public async Task<List<DishForCustomerResponse>> GetAllDishForCustomerAsync()
    {
        var dishes = (await repository.GetAll()).ToList();

        // 1️⃣ Витягуємо всі унікальні бізнеси
        var businessIds = dishes
            .Where(d => d.BusinessId != Guid.Empty)
            .Select(d => d.BusinessId)
            .Distinct()
            .ToList();

        // 2️⃣ Робимо fan-out RPC виклики паралельно
        var businessTasks = businessIds.ToDictionary(
            id => id,
            id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id))
        );

        await Task.WhenAll(businessTasks.Values);

        // 3️⃣ Формуємо словник BusinessId -> BusinessResponse
        var businesses = businessTasks.ToDictionary(
            x => x.Key,
            x => new BusinessResponse(
                x.Value.Result.AccountId,
                x.Value.Result.Name,
                x.Value.Result.Description
            )
        );

        // 4️⃣ Підтягуємо інгредієнти для кожної страви
        var result = new List<DishForCustomerResponse>();
        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            var businessDetails = businesses.GetValueOrDefault(d.BusinessId);

            result.Add(new DishForCustomerResponse(
                d.Id,
                d.MenuId,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category,
                d.CookingTime,
                businessDetails,
                ingredients
            ));
        }
        return result.Where(r => r.BusinessDetails != null).ToList();
    }

    public async Task<List<DishForCustomerResponse>> GetDishesForCustomerByBusinessIdAsync(Guid businessId)
    {
        var dishes = await repository.GetByBusinessIdAsync(businessId);
        
        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(dishes.First().BusinessId));
        
        var businessDetails = new  BusinessResponse(businessResponse.AccountId, businessResponse.Name, businessResponse.Description);

        var result = new List<DishForCustomerResponse>();
        foreach (var d in dishes)
        {
            var ingredients = await ingredientService.GetAllIngredientsByDishId(d.Id);

            result.Add(new DishForCustomerResponse(
                d.Id,
                d.MenuId,
                d.Name,
                d.Description,
                d.Image,
                d.Price,
                d.Category,
                d.CookingTime,
                businessDetails,
                ingredients
            ));
        }

        return result;
    }

}
