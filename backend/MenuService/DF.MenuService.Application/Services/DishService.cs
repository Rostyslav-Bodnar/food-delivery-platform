using DF.Contracts.Gateway.Requests.Dish;
using DF.Contracts.Gateway.Responses.Dish;
using DF.Contracts.RPC.Requests.UserService;
using DF.MenuService.Application.Mappers;
using DF.MenuService.Application.Messaging;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using System.Text.Json;
using DF.MenuService.Contracts.Exceptions;
using DF.MenuService.Contracts.Pagination;
using DF.MenuService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace DF.MenuService.Application.Services;

public class DishService(
    IDishRepository repository,
    ICloudinaryService cloudinaryService,
    IIngredientService ingredientService,
    UserServiceRpcClient userServiceRpcClient,
    IDistributedCache cache)
    : IDishService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(90);
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    private static string CustomerListKey(int page, int pageSize)
        => $"menu:dishes:customer:all:p{page}:s{pageSize}";

    private static string CustomerByBusinessKey(Guid businessId, int page, int pageSize)
        => $"menu:dishes:customer:business:{businessId}:p{page}:s{pageSize}";

    private async Task<T?> TryGetCachedAsync<T>(string key)
    {
        try
        {
            var raw = await cache.GetStringAsync(key);
            return raw is null ? default : JsonSerializer.Deserialize<T>(raw, CacheJsonOptions);
        }
        catch
        {
            return default; // never fail a request because the cache hiccupped
        }
    }

    private async Task TrySetCachedAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, CacheJsonOptions);
            await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        }
        catch
        {
            // best-effort write
        }
    }

    private async Task InvalidateCustomerCachesAsync(Guid? businessId = null)
    {
        // The cache key set is hash-based; for now we rely on TTL + key-prefix wildcard.
        // StackExchange.Redis doesn't expose a wildcard delete via IDistributedCache, so we
        // bump a per-business version stamp for targeted invalidation instead — simplest
        // pragmatic option: short TTL (90s) makes stale reads bounded without an extra index.
        try
        {
            if (businessId.HasValue)
            {
                // best-effort: remove the first few canonical pages so common reads refresh immediately.
                for (var p = 1; p <= 5; p++)
                {
                    await cache.RemoveAsync(CustomerByBusinessKey(businessId.Value, p, 20));
                    await cache.RemoveAsync(CustomerListKey(p, 20));
                }
            }
        }
        catch
        {
            // best-effort
        }
    }

    public async Task<DishResponse> CreateDishAsync(CreateDishRequest request)
    {
        var accountResponse = await userServiceRpcClient.GetBusinessAccountAsync(
            new GetBusinessAccountRequest(request.BusinessId));

        if (accountResponse == null)
            throw new NotFoundException("Business account not found");

        if (!accountResponse.StripeChargesEnabled || !accountResponse.StripePayoutsEnabled)
            throw new StripeAccountNotReadyException();

        if (!string.IsNullOrWhiteSpace(accountResponse.StripeRequirementsDue))
            throw new StripeAccountNotReadyException("Stripe requirements are not completed.");

        string? imageUrl = null;
        string? imagePublicId = null;

        if (request.Image != null)
        {
            var upload = await cloudinaryService.UploadAsync(request.Image, "dishes");
            imageUrl = upload.Url;
            imagePublicId = upload.PublicId;
        }

        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description,
            Image = imageUrl,
            ImagePublicId = imagePublicId,
            Price = request.Price,
            Category = request.Category.ToDomain(),
            BusinessId = request.BusinessId
        };

        var ingredients = request.Ingredients.Select(i => new Ingredient
        {
            DishId = dish.Id,
            Name = i.Name,
            Weight = i.Weight
        }).ToList();

        try
        {
            await repository.CreateWithIngredientsAsync(dish, ingredients);
        }
        catch
        {
            // Compensating action: the asset is uploaded but the DB write failed.
            if (!string.IsNullOrWhiteSpace(imagePublicId))
            {
                await TryDeleteCloudinaryAssetAsync(imagePublicId);
            }
            throw;
        }

        await InvalidateCustomerCachesAsync(dish.BusinessId);

        var ingredientResponses = ingredients
            .Select(i => new IngredientResponse(i.Id, i.DishId, i.Name, i.Weight))
            .ToList();

        return new DishResponse(
            dish.Id,
            dish.Name,
            dish.Description,
            dish.Image,
            dish.Price,
            dish.Category.ToContract(),
            dish.CookingTime,
            ingredientResponses
        );
    }

    public async Task<IEnumerable<DishResponse>> GetAllAsync()
    {
        var dishes = await repository.GetAllWithIngredientsAsync();
        return dishes.Select(MapToDishResponse).ToList();
    }

    public async Task<DishResponse?> GetByIdAsync(Guid id)
    {
        var dish = await repository.GetWithIngredientsAsync(id);
        return dish is null ? null : MapToDishResponse(dish);
    }

    public async Task<IEnumerable<DishResponse>> GetByBusinessId(Guid businessId)
    {
        var dishes = await repository.GetByBusinessIdWithIngredientsAsync(businessId);
        return dishes.Select(MapToDishResponse).ToList();
    }

    private static DishResponse MapToDishResponse(Dish d) => new(
        d.Id,
        d.Name,
        d.Description,
        d.Image,
        d.Price,
        d.Category.ToContract(),
        d.CookingTime,
        d.Ingredients
            .Select(i => new IngredientResponse(i.Id, i.DishId, i.Name, i.Weight))
            .ToList());

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await repository.Get(id);

        if (existing == null)
            throw new NotFoundException($"Dish {id} not found");

        var publicIdToDelete = existing.ImagePublicId;

        var deleted = await repository.Delete(id);

        if (deleted && !string.IsNullOrWhiteSpace(publicIdToDelete))
        {
            await TryDeleteCloudinaryAssetAsync(publicIdToDelete);
        }

        if (deleted)
        {
            await InvalidateCustomerCachesAsync(existing.BusinessId);
        }

        return deleted;
    }

    public async Task<DishResponse> UpdateDishAsync(UpdateDishRequest request)
    {
        var existing = await repository.Get(request.DishId);

        if (existing == null)
            throw new NotFoundException($"Dish {request.DishId} not found");

        string? imageUrl = existing.Image;
        string? imagePublicId = existing.ImagePublicId;
        string? previousPublicId = null;

        if (request.Image != null)
        {
            var upload = await cloudinaryService.UploadAsync(request.Image, "dishes");
            previousPublicId = existing.ImagePublicId;
            imageUrl = upload.Url;
            imagePublicId = upload.PublicId;
        }

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.Category = request.Category.ToDomain();
        existing.Image = imageUrl;
        existing.ImagePublicId = imagePublicId;
        existing.CookingTime = request.CookingTime;

        await repository.Update(existing);

        // Only delete the previous asset after the DB has accepted the new URL.
        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await TryDeleteCloudinaryAssetAsync(previousPublicId);
        }

        await InvalidateCustomerCachesAsync(existing.BusinessId);

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

    private async Task TryDeleteCloudinaryAssetAsync(string publicId)
    {
        try
        {
            await cloudinaryService.DeleteAsync(publicId);
        }
        catch
        {
            // Storage cleanup is best-effort; the DB is the source of truth.
            // A janitor job can sweep up unreferenced PublicIds later if needed.
        }
    }

    public async Task<DishForCustomerResponse> GetDishForCustomerAsync(Guid dishId)
    {
        var dish = await repository.GetWithIngredientsAsync(dishId)
                   ?? throw new NotFoundException($"Dish {dishId} not found");

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(
                                   new GetBusinessAccountRequest(dish.BusinessId))
                               ?? throw new NotFoundException("Business not found");

        var businessDetails = new BusinessResponse(
            businessResponse.AccountId,
            businessResponse.Name,
            businessResponse.Description);

        return MapToDishForCustomerResponse(dish, businessDetails);
    }

    public async Task<List<DishForCustomerResponse>> GetAllDishForCustomerAsync()
    {
        var dishes = await repository.GetAllWithIngredientsAsync();
        if (dishes.Count == 0) return [];

        var businessIds = dishes
            .Where(d => d.BusinessId != Guid.Empty)
            .Select(d => d.BusinessId)
            .Distinct()
            .ToList();

        var businessTasks = businessIds.ToDictionary(
            id => id,
            id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)));

        await Task.WhenAll(businessTasks.Values);

        var businesses = new Dictionary<Guid, BusinessResponse>();
        foreach (var (id, task) in businessTasks)
        {
            var result = await task;
            if (result == null) continue; // skip dishes whose business we couldn't resolve
            businesses[id] = new BusinessResponse(result.AccountId, result.Name, result.Description);
        }

        var response = new List<DishForCustomerResponse>(dishes.Count);
        foreach (var dish in dishes)
        {
            if (!businesses.TryGetValue(dish.BusinessId, out var businessDetails)) continue;
            response.Add(MapToDishForCustomerResponse(dish, businessDetails));
        }
        return response;
    }

    public async Task<List<DishForCustomerResponse>> GetDishesForCustomerByBusinessIdAsync(Guid businessId)
    {
        var dishes = await repository.GetByBusinessIdWithIngredientsAsync(businessId);
        if (dishes.Count == 0) return [];

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(
                                   new GetBusinessAccountRequest(businessId))
                               ?? throw new NotFoundException("Business not found");

        var businessDetails = new BusinessResponse(
            businessResponse.AccountId,
            businessResponse.Name,
            businessResponse.Description);

        return dishes.Select(d => MapToDishForCustomerResponse(d, businessDetails)).ToList();
    }

    private static DishForCustomerResponse MapToDishForCustomerResponse(Dish d, BusinessResponse business) => new(
        d.Id,
        d.Name,
        d.Description,
        d.Image,
        d.Price,
        d.Category.ToContract(),
        d.CookingTime,
        business,
        d.Ingredients
            .Select(i => new IngredientResponse(i.Id, i.DishId, i.Name, i.Weight))
            .ToList());

    public async Task<PagedResponse<DishResponse>> GetPagedAsync(PageRequest page)
    {
        var (items, total) = await repository.GetPagedWithIngredientsAsync(page.Skip, page.PageSize);
        var mapped = items.Select(MapToDishResponse).ToList();
        return new PagedResponse<DishResponse>(mapped, page.Page, page.PageSize, total);
    }

    public async Task<PagedResponse<DishForCustomerResponse>> GetAllForCustomerPagedAsync(PageRequest page)
    {
        var cacheKey = CustomerListKey(page.Page, page.PageSize);
        var cached = await TryGetCachedAsync<PagedResponse<DishForCustomerResponse>>(cacheKey);
        if (cached is not null) return cached;

        var (items, total) = await repository.GetPagedWithIngredientsAsync(page.Skip, page.PageSize);
        if (items.Count == 0)
            return new PagedResponse<DishForCustomerResponse>([], page.Page, page.PageSize, total);

        var businesses = await ResolveBusinessesAsync(items.Select(d => d.BusinessId));
        var mapped = items
            .Where(d => businesses.ContainsKey(d.BusinessId))
            .Select(d => MapToDishForCustomerResponse(d, businesses[d.BusinessId]))
            .ToList();

        var response = new PagedResponse<DishForCustomerResponse>(mapped, page.Page, page.PageSize, total);
        await TrySetCachedAsync(cacheKey, response);
        return response;
    }

    public async Task<PagedResponse<DishForCustomerResponse>> GetForCustomerByBusinessPagedAsync(Guid businessId, PageRequest page)
    {
        var cacheKey = CustomerByBusinessKey(businessId, page.Page, page.PageSize);
        var cached = await TryGetCachedAsync<PagedResponse<DishForCustomerResponse>>(cacheKey);
        if (cached is not null) return cached;

        var (items, total) = await repository.GetByBusinessIdPagedWithIngredientsAsync(businessId, page.Skip, page.PageSize);
        if (items.Count == 0)
            return new PagedResponse<DishForCustomerResponse>([], page.Page, page.PageSize, total);

        var businessResponse = await userServiceRpcClient.GetBusinessAccountAsync(
                                   new GetBusinessAccountRequest(businessId))
                               ?? throw new NotFoundException("Business not found");

        var businessDetails = new BusinessResponse(
            businessResponse.AccountId, businessResponse.Name, businessResponse.Description);

        var mapped = items.Select(d => MapToDishForCustomerResponse(d, businessDetails)).ToList();
        var response = new PagedResponse<DishForCustomerResponse>(mapped, page.Page, page.PageSize, total);
        await TrySetCachedAsync(cacheKey, response);
        return response;
    }

    private async Task<Dictionary<Guid, BusinessResponse>> ResolveBusinessesAsync(IEnumerable<Guid> businessIds)
    {
        var ids = businessIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var tasks = ids.ToDictionary(
            id => id,
            id => userServiceRpcClient.GetBusinessAccountAsync(new GetBusinessAccountRequest(id)));
        await Task.WhenAll(tasks.Values);

        var result = new Dictionary<Guid, BusinessResponse>(ids.Count);
        foreach (var (id, task) in tasks)
        {
            var resp = await task;
            if (resp == null) continue;
            result[id] = new BusinessResponse(resp.AccountId, resp.Name, resp.Description);
        }
        return result;
    }
}