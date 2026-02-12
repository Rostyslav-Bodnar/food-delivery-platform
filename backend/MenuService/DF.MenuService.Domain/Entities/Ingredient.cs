namespace DF.MenuService.Domain.Entities;

public class Ingredient
{
    public Guid Id { get; set; }
    public required Guid DishId { get; set; }
    public Dish Dish {get; set;}
    public required string Name { get; set; }
    public required int Weight { get; set; }
}