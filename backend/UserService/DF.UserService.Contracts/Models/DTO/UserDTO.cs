namespace DF.UserService.Contracts.Models.DTO
{
    public record UserDto(Guid Id, string Email, string FullName, string UserRole, AccountDTO CurrentAccount);
}
