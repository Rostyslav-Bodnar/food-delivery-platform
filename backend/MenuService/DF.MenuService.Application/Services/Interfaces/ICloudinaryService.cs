using DF.MenuService.Contracts.Models.Response;
using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Application.Services.Interfaces;

public interface ICloudinaryService
{
    Task<UploadImageResult> UploadAsync(IFormFile file, string folder = null);
    Task<bool> DeleteAsync(string publicId);
}