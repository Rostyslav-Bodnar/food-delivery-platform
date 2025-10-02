using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Interfaces;

public interface IAccountService
{
    Task<AccountDTO> CreateAccountAsync(Guid userId, AccountType accountType, string? imageURL);
    Task<AccountDTO> UpdateAccountAsync(AccountDTO account);
    Task<bool> DeleteAccountAsync(Guid id);
    Task<AccountDTO?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<AccountDTO>?> GetAccountsByUserAsync(Guid userId);
}