using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Response;
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
        UploadImageResult? uploadedImage = null;

        try
        {
            var user = await userRepository.Get(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            if (accountRequest.ImageFile is { Length: > 0 })
            {
                uploadedImage = await cloudinaryService.UploadAsync(accountRequest.ImageFile, "users");
            }

            var entity = accountFactory.CreateAccount(accountRequest, userId, uploadedImage);
            entity = await accountRepository.Create(entity);

            // Promote the new account to the user's active account so the next-issued
            // JWT carries account_id + account_type claims for this account (which the
            // gateway forwards as X-Internal-AccountId / X-Internal-AccountType). Without
            // this step, downstream services (e.g. TrackingService) would still see the
            // user's original Customer account and reject Business-only mutations.
            user.AccountId = entity.Id;
            await userRepository.Update(user);

            // Business accounts are persisted with StripeAccountId == null. The
            // StripeAccountProvisioningWorker picks them up and provisions the
            // Stripe Express account out-of-band so the request path stays fast
            // and doesn't fail when Stripe is down.

            return entity switch
            {
                CustomerAccount c => AccountMapper.ToDTO(c),
                BusinessAccount b => AccountMapper.ToDTO(b),
                CourierAccount co => AccountMapper.ToDTO(co),
                _ => AccountMapper.ToDTO(entity)
            };
        }
        catch (Exception ex)
        {
            // If the upload succeeded but the entity save failed, the Cloudinary
            // asset would be orphaned. Best-effort cleanup.
            if (uploadedImage is not null)
            {
                try
                {
                    await cloudinaryService.DeleteAsync(uploadedImage.PublicId);
                }
                catch
                {
                    // Swallow — surface the original failure.
                }
            }

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
    
    public async Task<OnboardingLinkResponse> GetOnboardingLinkAsync(Guid businessId, CancellationToken ct)
    {
        var entity = await accountRepository.Get(businessId) as BusinessAccount
                     ?? throw new KeyNotFoundException("Business not found");

        if (string.IsNullOrWhiteSpace(entity.StripeAccountId))
        {
            // StripeAccountProvisioningWorker hasn't populated the account
            // yet. Tell the caller to retry instead of erroring out.
            return OnboardingLinkResponse.Provisioning();
        }

        var url = await stripe.CreateOnboardingLinkAsync(
            entity.StripeAccountId,
            stripeOptions.Dashboard.ReturnUrl,
            stripeOptions.Dashboard.RefreshUrl,
            ct);

        return OnboardingLinkResponse.Ready(url);
    }


}
