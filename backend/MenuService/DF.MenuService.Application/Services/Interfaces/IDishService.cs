using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IDishService
{
    Task<DishResponse> CreateDishAsync(CreateDishRequest request);
    Task<IEnumerable<DishResponse>> GetAllAsync();
}