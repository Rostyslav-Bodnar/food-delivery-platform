namespace DF.UserService.Domain.Entities;

public class BusinessAccount : Account
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
}