namespace DF.UserService.Contracts.Models.DTO;

public record ProfileDTO(UserDto User, AccountDTO CurrentAccount, IEnumerable<AccountDTO> Accounts);
