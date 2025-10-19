namespace DF.MenuService.Contracts.Models.Response;

public record MenuResponse(Guid Id, Guid BusinessId, string? Name,  string? Image);