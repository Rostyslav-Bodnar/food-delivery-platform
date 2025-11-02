namespace DF.MenuService.Domain.Entities;

public class Dish
{
    public Guid Id { get; set; }
    public Guid? MenuId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public Guid BusinessId { get; set; }
    
    public int CookingTime { get; set; }
}