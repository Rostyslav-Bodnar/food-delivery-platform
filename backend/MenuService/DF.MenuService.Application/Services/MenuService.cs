using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;
using DF.MenuService.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Application.Services;

public class MenuService(IMenuRepository menuRepository, ICloudinaryService cloudinaryService) : IMenuService
{
    public async Task<IEnumerable<MenuResponse>> GetAllAsync()
    {
        var entity = await menuRepository.GetAll();
        return entity.
            Select(m => 
                new MenuResponse(m.Id, m.BusinessId, m.Name, m.Image));
        
    }

    public async Task<MenuResponse?> GetAsync(Guid id)
    {
        var menu = await menuRepository.Get(id);
        if (menu == null)
            return null;

        return new MenuResponse(menu.Id, menu.BusinessId, menu.Name, menu.Image);
    }

    public async Task<MenuResponse> CreateAsync(CreateMenuRequest menu, IFormFile? imageFile = null)
    {
        var imageUrl = "";
        if (imageFile != null)
        {
            var uploadResult = await cloudinaryService.UploadAsync(imageFile, "menus");
            imageUrl = uploadResult.Url;
        }

        var entity = new Menu
        {
            BusinessId = menu.BusinessId,
            Name = menu.Name,
            Image = imageUrl
        };

        var created = await menuRepository.Create(entity);

        return new MenuResponse(created.Id, created.BusinessId, created.Name, created.Image);
    }
}