using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using Microsoft.AspNetCore.Http;

namespace DF.UserService.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest  accountRequest);
    Task<AccountResponse> UpdateAccountAsync(CreateAccountRequest accountRequest);
    Task<bool> DeleteAccountAsync(Guid id);
    Task<AccountResponse?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<AccountResponse>?> GetAccountsByUserAsync(Guid userId);
}