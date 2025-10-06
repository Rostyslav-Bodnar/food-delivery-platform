using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Interfaces;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Services;

public class AccountService(IAccountRepository accountRepository, IAccountFactory accountFactory) : IAccountService
{
    public async Task<AccountDTO> CreateAccountAsync(AccountDTO accountDto)
    {
        try
        {
            var entity = accountFactory.CreateAccount(accountDto);

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
            throw new ApplicationException($"Error creating account for user {accountDto.UserId}", ex);
        }
    }

    public async Task<AccountDTO> UpdateAccountAsync(AccountDTO account)
    {
        try
        {
            var entity = AccountMapper.ToEntity(account);
            await accountRepository.Update(entity);
            return AccountMapper.ToDTO(entity);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error updating account with Id {account}", ex);
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

    public async Task<AccountDTO?> GetAccountByUserAsync(Guid userId)
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

    public async Task<IEnumerable<AccountDTO>?> GetAccountsByUserAsync(Guid userId)
    {
        try
        {
            var entities = (await accountRepository.GetAccountsByUserAsync(userId)).ToList();

            if (!entities.Any())
                return Enumerable.Empty<AccountDTO>();

            return entities
                .Select(AccountMapper.ToDTO)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving accounts for user {userId}", ex);
        }
    }

}
