using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountMapper
{
    public static AccountDTO ToDTO(Account entity)
    {
        var dto = new AccountDTO(entity.Id,  entity.UserId.ToString(), entity.AccountType.ToString());
        return dto;
    }

    public static Account ToEntity(AccountDTO dto)
    {
        var entity = new Account
        {
            UserId = dto.Id,
            AccountType = (AccountType)Enum.Parse(typeof(AccountType), dto.AccountType)
        };
        
        return entity;
    }
}