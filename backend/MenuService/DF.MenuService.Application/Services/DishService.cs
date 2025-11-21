using DF.Contracts.RPC.Requests;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;
using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Services;

public class DishService(IDishRepository repository, ICloudinaryService cloudinaryService, UserServiceRpcClient  userServiceRpcClient)
    : IDishService
{
    public async Task<DishResponse> CreateDishAsync(CreateDishRequest request)
    {
        var accountResponse = await userServiceRpcClient.GetAccountAsync(new GetAccountRequest(request.UserId));
        
        string? imageUrl = null;
        if (request.Image != null)
        {
            var uploadResult = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = uploadResult.Url;
        }

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            MenuId = request.MenuId,
            Name = request.Name,
            Description = request.Description,
            Image = imageUrl,
            Price = request.Price,
            Category = request.Category,
            BusinessId = accountResponse.AccountId
        };

        await repository.Create(dish);

        return new DishResponse(dish.Id, dish.MenuId, dish.Name, dish.Description, dish.Image, dish.Price,
            dish.Category);
    }
    
    public async Task<IEnumerable<DishResponse>> GetAllAsync()
    {
        var dishes = await repository.GetAll();
        return dishes.Select(d => new DishResponse
        (
            d.Id, 
            d.MenuId, 
            d.Name, 
            d.Description, 
            d.Image, 
            d.Price,
            d.Category
            ));
    }
    public async Task<DishResponse> GetByIdAsync(Guid id)
    {
        var dish = await repository.Get(id);
        return new DishResponse(
            dish.Id,
            dish.MenuId, 
            dish.Name,
            dish.Description, 
            dish.Image, 
            dish.Price,
            dish.Category);
    }

    public async Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId)
    {
        var dishes = await repository.GetByBusinessIdAsync(businessId);
        return dishes.Select(d => 
            new DishResponse(d.Id, d.MenuId, d.Name, d.Description, d.Image, d.Price, d.Category));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.Delete(id);
    }

    public async Task<DishResponse> UpdateDishAsync(UpdateDishRequest request)
    {
        string? imageUrl = null;
        if (request.Image != null)
        {
            var uploadResult = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = uploadResult.Url;
        }
        
        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            MenuId = request.MenuId,
            Name = request.Name,
            Description = request.Description,
            Image = imageUrl,
            Price = request.Price,
            Category = request.Category
        };

        var result = await repository.Update(dish);
            
        return new DishResponse(result.Id, result.MenuId, result.Name, result.Description, result.Image, result.Price,
            result.Category);
    }

}