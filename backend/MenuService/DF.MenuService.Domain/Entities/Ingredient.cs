namespace DF.MenuService.Domain.Entities;

public class Ingredient
{
    public Guid Id { get; set; }
    public Guid DishId { get; set; }
    public required Dish Dish {get; set;}
    public required string Name { get; set; }
    public required int Weight { get; set; }
}