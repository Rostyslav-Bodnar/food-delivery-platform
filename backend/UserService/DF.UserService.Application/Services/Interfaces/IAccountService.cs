using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;

namespace DF.UserService.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest accountRequest, Guid userId);
    Task<AccountResponse> UpdateAccountAsync(UpdateAccountRequest accountRequest);
    Task<bool> DeleteAccountAsync(Guid id);
    Task<AccountResponse?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<AccountResponse>?> GetAccountsByUserAsync(Guid userId);
    Task<IEnumerable<AccountResponse>?> GetBusinessAccountsAsync();
    Task<string> GetOnboardingLinkAsync(Guid businessId, CancellationToken ct);
}