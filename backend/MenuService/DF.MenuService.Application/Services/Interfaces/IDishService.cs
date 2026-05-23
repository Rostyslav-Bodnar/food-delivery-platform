using DF.Contracts.Gateway.Requests.Dish;
using DF.Contracts.Gateway.Responses.Dish;
using DF.MenuService.Contracts.Pagination;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IDishService
{
    Task<DishResponse> CreateDishAsync(CreateDishRequest request);

    // Non-paged versions kept for internal callers (RPC consumers, etc.).
    Task<IEnumerable<DishResponse>> GetAllAsync();
    Task<DishResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId);
    Task<DishForCustomerResponse> GetDishForCustomerAsync(Guid dishId);
    Task<List<DishForCustomerResponse>> GetAllDishForCustomerAsync();
    Task<List<DishForCustomerResponse>> GetDishesForCustomerByBusinessIdAsync(Guid businessId);

    // Paged variants for public HTTP endpoints.
    Task<PagedResponse<DishResponse>> GetPagedAsync(PageRequest page);
    Task<PagedResponse<DishForCustomerResponse>> GetAllForCustomerPagedAsync(PageRequest page);
    Task<PagedResponse<DishForCustomerResponse>> GetForCustomerByBusinessPagedAsync(Guid businessId, PageRequest page);

    Task<bool> DeleteAsync(Guid id);
    Task<DishResponse> UpdateDishAsync(UpdateDishRequest request);
}
