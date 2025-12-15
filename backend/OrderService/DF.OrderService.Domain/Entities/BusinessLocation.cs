namespace DF.OrderService.Domain.Entities;

public class BusinessLocation
{
    public Guid Id {get; set;}
    public Guid LocationId { get; set; }
    public Location Location { get; set; }
    public Guid BusinessId { get; set; }
}