namespace DF.OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OrderedBy { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Guid DeliverToId { get; set; }
    public Location DeliverTo { get; set; }
    public Guid DeliverFromId { get; set; }
    public Location DeliverFrom { get; set; }
    public Guid? DeliveredBy {get; set;}
    public OrderStatus OrderStatus { get; set; }
    
    public string OrderNumber { get; set; }
}