namespace DF.OrderService.Domain.Entities;

public class OrderedDish
{
    public Guid Id { get; set; }
    public required Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid DishId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string DishName { get; set; } = string.Empty;
}