namespace DF.OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OrderedBy { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Guid? DeliverToId { get; set; }
    public Guid? DeliverFromId { get; set; }
    public Guid? DeliveredById {get; set;}
    public OrderStatus OrderStatus { get; set; }
    
    public string OrderNumber { get; set; }
}