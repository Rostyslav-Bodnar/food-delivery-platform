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
    public decimal DeliveryFee { get; set; }
    public decimal CourierFee { get; set; }
    public bool CourierPaid { get; set; }
    
    public string OrderNumber { get; set; }
    public decimal Profit { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
}
