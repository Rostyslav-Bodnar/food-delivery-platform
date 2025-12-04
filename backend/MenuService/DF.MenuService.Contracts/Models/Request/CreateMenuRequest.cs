using Microsoft.AspNetCore.Http;

namespace DF.MenuService.Contracts.Models.Request;

public record CreateMenuRequest(Guid BusinessId, string? Name, IFormFile? ImageFile);