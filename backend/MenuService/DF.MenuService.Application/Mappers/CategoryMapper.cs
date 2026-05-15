using DF.Contracts.Enums;

namespace DF.MenuService.Application.Mappers;

public static class CategoryMapper
{
    public static Domain.Entities.Category ToDomain(this Category category)
        => (Domain.Entities.Category)category;

    public static Category ToContract(this Domain.Entities.Category category)
        => (Category)category;
}
