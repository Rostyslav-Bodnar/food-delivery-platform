namespace DF.UserService.Contracts.Models.DTO
{
    public record AccountDTO(string? Id, string UserId, string AccountType, string? ImageURL = null);
    
    //TODO: create DTO for accounts
}
