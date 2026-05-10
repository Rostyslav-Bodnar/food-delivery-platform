

using DF.Contracts.Gateway.Requests.Dish;
using DF.Contracts.Gateway.Responses.Dish;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IDishService
{
    Task<DishResponse> CreateDishAsync(CreateDishRequest request);
    Task<IEnumerable<DishResponse>> GetAllAsync();
    Task<DishResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId);
    Task<DishForCustomerResponse> GetDishForCustomerAsync(Guid dishId);
    Task<List<DishForCustomerResponse>> GetAllDishForCustomerAsync();
    Task<List<DishForCustomerResponse>> GetDishesForCustomerByBusinessIdAsync(Guid businessId);

    Task<bool> DeleteAsync(Guid id);

    Task<DishResponse> UpdateDishAsync(UpdateDishRequest request);
}