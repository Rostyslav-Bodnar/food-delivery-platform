namespace DF.UserService.Domain.Entities;

public class CourierAccount : Account
{
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public string? Address { get; set; } 
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }

}