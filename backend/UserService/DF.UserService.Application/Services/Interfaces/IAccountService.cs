using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Contracts.Models.Response;

namespace DF.UserService.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest accountRequest, Guid userId);
    Task<AccountResponse> UpdateAccountAsync(UpdateAccountRequest accountRequest);
    Task<bool> DeleteAccountAsync(Guid id);
    Task<AccountResponse?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<AccountResponse>?> GetAccountsByUserAsync(Guid userId);
    Task<IEnumerable<AccountResponse>?> GetBusinessAccountsAsync();
    Task<OnboardingLinkResponse> GetOnboardingLinkAsync(Guid businessId, CancellationToken ct);
}