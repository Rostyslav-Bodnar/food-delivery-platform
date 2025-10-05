namespace DF.UserService.Contracts.Models.DTO
{
    public record UserDto(Guid Id, string Email, string Name, string Surname, string UserRole, AccountDTO CurrentAccount);
}
