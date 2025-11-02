using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Services;

public class AccountService(
    IAccountRepository accountRepository, 
    IAccountFactory accountFactory,
    ICloudinaryService cloudinaryService) : IAccountService
{
    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest accountRequest, Guid userId)
    {
        try
        {
            var entity = await accountFactory.CreateAccount(accountRequest, userId);
            
            entity = await accountRepository.Create(entity);

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
    
    private string? ExtractPublicIdFromUrl(string imageUrl)
    {
        // Наприклад, якщо URL = https://res.cloudinary.com/demo/image/upload/v123456/accounts/abc123.jpg
        // Ми беремо "accounts/abc123" як PublicId
        try
        {
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');
            if (segments.Length >= 3)
            {
                // видаляємо розширення файлу
                var filename = segments[^1];
                var publicIdWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                return $"{segments[^2]}/{publicIdWithoutExtension}"; // "accounts/abc123"
            }
        }
        catch { }

        return null;
    }


}
