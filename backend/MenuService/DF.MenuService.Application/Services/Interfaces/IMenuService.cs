using DF.MenuService.Contracts.Models.Request;
using DF.MenuService.Contracts.Models.Response;
using DF.MenuService.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Application.Services.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<MenuResponse>> GetAllAsync();
    Task<MenuResponse?> GetAsync(Guid id);
    Task<MenuResponse> CreateAsync(CreateMenuRequest menu, IFormFile? imageFile = null);
}