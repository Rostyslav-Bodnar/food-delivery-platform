using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IDishService
{
    Task<DishResponse> CreateDishAsync(CreateDishRequest request);
    Task<IEnumerable<DishResponse>> GetAllAsync();
    Task<DishResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId);
    Task<bool> DeleteAsync(Guid id);

    Task<DishResponse> UpdateDishAsync(UpdateDishRequest request);
}