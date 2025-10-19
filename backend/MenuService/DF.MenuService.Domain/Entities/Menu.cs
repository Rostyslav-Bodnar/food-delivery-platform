namespace DF.MenuService.Domain.Entities;

public class Menu
{
    public Guid Id { get; set; }
    public Guid BusinessId  { get; set; }
    public string? Name { get; set; }
    public string? Image { get; set; }
}