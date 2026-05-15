using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.Options;
using AccountResponse = DF.Contracts.Gateway.Responses.AccountResponse;

namespace DF.UserService.Application.Services;

public class AccountService(
    IAccountRepository accountRepository,
    IUserRepository userRepository,
    IAccountFactory accountFactory,
    ICloudinaryService cloudinaryService,
    IStripeConnectService stripe,
    IOptions<StripeOptions> stripeOptions)
    : IAccountService
{
    private readonly StripeOptions stripeOptions = stripeOptions.Value;


    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest accountRequest, Guid userId)
    {
        try
        {
            var entity = await accountFactory.CreateAccount(accountRequest, userId);
            var user = await userRepository.Get(userId);
            if (user == null)
                throw new NullReferenceException("User not found.");
            
            entity = await accountRepository.Create(entity);

            if (entity is BusinessAccount businessAccount)
            {
                
                // 1) створити Connected Account
                var stripeAccountId = await stripe.CreateExpressAccountAsync(user.Email ?? "", stripeOptions.DefaultCountry);
                businessAccount.StripeAccountId = stripeAccountId;

                // 2) витягнути статус
                var status = await stripe.GetAccountStatusAsync(stripeAccountId);
                businessAccount.StripeChargesEnabled = status.ChargesEnabled;
                businessAccount.StripePayoutsEnabled = status.PayoutsEnabled;
                businessAccount.StripeRequirementsDue = status.RequirementsDue;

                await accountRepository.Update(businessAccount);

            }
            
            return entity switch
            {
                CustomerAccount c => AccountMapper.ToDTO((CustomerAccount)c),
                BusinessAccount b => AccountMapper.ToDTO((BusinessAccount)b),
                CourierAccount co => AccountMapper.ToDTO((CourierAccount)co),
                _ => AccountMapper.ToDTO(entity) // fallback
            };
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error creating account for user {accountRequest.AccountType}", ex);
        }
    }

    public async Task<AccountResponse> UpdateAccountAsync(UpdateAccountRequest accountRequest)
    {
        try
        {
            var existingAccount = await accountRepository.Get(Guid.Parse(accountRequest.Id));
            if (existingAccount == null)
                throw new ApplicationException($"Account with Id {accountRequest.Id} not found.");

            string? imageUrl = existingAccount.ImageUrl;
            string? publicId = existingAccount.ImagePublicId; 

            if (accountRequest.ImageFile is { Length: > 0 })
            {
                if (!string.IsNullOrEmpty(publicId))
                    await cloudinaryService.DeleteAsync(publicId);

                var uploadResult = await cloudinaryService.UploadAsync(accountRequest.ImageFile, "users");
                imageUrl = uploadResult.Url;
                publicId = uploadResult.PublicId;
            }

            var updatedEntity = UpdatedAccountMapper.ToEntity(accountRequest, existingAccount, imageUrl);
            updatedEntity.ImagePublicId = publicId; 

            await accountRepository.Update(updatedEntity);

            return AccountMapper.ToDTO(updatedEntity);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error updating account with Id {accountRequest.Id}", ex);
        }
    }
    
    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        try
        {
            await accountRepository.Delete(id);
            return true;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error deleting account with Id {id}", ex);
        }
    }

    public async Task<AccountResponse?> GetAccountByUserAsync(Guid userId)
    {
        try
        {
            var entity = await accountRepository.GetAccountByUserAsync(userId);
            return entity == null ? null : AccountMapper.ToDTO(entity);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving account for user {userId}", ex);
        }
    }

    public async Task<IEnumerable<AccountResponse>?> GetAccountsByUserAsync(Guid userId)
    {
        try
        {
            var entities = (await accountRepository.GetAccountsByUserAsync(userId)).ToList();

            if (!entities.Any())
                return Enumerable.Empty<AccountResponse>();

            return entities
                .Select(AccountMapper.ToDTO)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving accounts for user {userId}", ex);
        }
    }

    public async Task<IEnumerable<AccountResponse>?> GetBusinessAccountsAsync()
    {
        var accounts = await accountRepository.GetAll();

        var businessAccounts = accounts
            .OfType<BusinessAccount>()
            .Select(b => AccountMapper.ToDTO(b));

        return businessAccounts.ToList();
    }
    
    public async Task<string> GetOnboardingLinkAsync(Guid businessId, CancellationToken ct)
    {
        var entity = await accountRepository.Get(businessId) as BusinessAccount
                     ?? throw new ApplicationException("Business not found");

        if (string.IsNullOrWhiteSpace(entity.StripeAccountId))
            throw new ApplicationException("StripeAccountId is not set");

        return await stripe.CreateOnboardingLinkAsync(
            entity.StripeAccountId,
            stripeOptions.Dashboard.ReturnUrl,
            stripeOptions.Dashboard.RefreshUrl,
            ct);
    }


}
