namespace DF.TrackingService.Domain.Entities;

public class CourierLocation
{
    public Guid Id { get; set; }
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public CourierStatus CourierStatus { get; set; }
}