namespace DF.UserService.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public User User {get; set;}

    public required AccountType AccountType { get; set; }

    public string? ImageUrl { get; set; }
    
    //TODO: Add score field from Quality Assessment DB 
}