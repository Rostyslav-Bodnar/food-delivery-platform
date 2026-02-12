namespace DF.UserService.Contracts.Models.DTO;

public record ProfileDTO(UserDto User, AccountResponse CurrentAccount, IEnumerable<AccountResponse> Accounts);
