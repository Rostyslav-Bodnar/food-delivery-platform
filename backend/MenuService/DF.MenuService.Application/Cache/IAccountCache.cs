namespace DF.MenuService.Application.Cache;

public interface IAccountCache
{
    Task<Guid?> GetBusinessIdAsync(Guid userId);
    Task SetBusinessIdAsync(Guid userId, Guid businessId);
}