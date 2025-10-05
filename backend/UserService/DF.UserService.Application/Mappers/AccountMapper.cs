using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountMapper
{
    // === ENTITY → DTO ===
    public static AccountDTO ToDTO(Account account)
    {
        return account switch
        {
            CourierAccount courier => new CourierAccountDTO(
                courier.Id.ToString(),
                courier.UserId.ToString(),
                courier.AccountType.ToString(),
                courier.ImageUrl,
                courier.PhoneNumber,
                courier.Name,
                courier.Surname,
                courier.Address,
                courier.Description
            ),
            CustomerAccount customer => new CustomerAccountDTO(
                customer.Id.ToString(),
                customer.UserId.ToString(),
                customer.AccountType.ToString(),
                customer.ImageUrl,
                customer.PhoneNumber,
                customer.Name,
                customer.Surname,
                customer.Address
            ),
            BusinessAccount business => new BusinessAccountDTO(
                business.Id.ToString(),
                business.UserId.ToString(),
                business.AccountType.ToString(),
                business.ImageUrl,
                business.Name,
                business.Description
            ),
            _ => throw new ArgumentException($"Unknown account type: {account.GetType().Name}")
        };
    }

    // === DTO → ENTITY ===
    public static Account ToEntity(AccountDTO dto)
    {
        return dto switch
        {
            CourierAccountDTO courier => new CourierAccount
            {
                Id = string.IsNullOrEmpty(courier.Id) ? Guid.NewGuid() : Guid.Parse(courier.Id),
                UserId = Guid.Parse(courier.UserId),
                AccountType = Enum.Parse<AccountType>(courier.AccountType),
                ImageUrl = courier.ImageUrl,
                PhoneNumber = courier.PhoneNumber,
                Name = courier.Name,
                Surname = courier.Surname,
                Address = courier.Address,
                Description = courier.Description
            },
            CustomerAccountDTO customer => new CustomerAccount
            {
                Id = string.IsNullOrEmpty(customer.Id) ? Guid.NewGuid() : Guid.Parse(customer.Id),
                UserId = Guid.Parse(customer.UserId),
                AccountType = Enum.Parse<AccountType>(customer.AccountType),
                ImageUrl = customer.ImageUrl,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                Surname = customer.Surname,
                Address = customer.Address
            },
            BusinessAccountDTO business => new BusinessAccount
            {
                Id = string.IsNullOrEmpty(business.Id) ? Guid.NewGuid() : Guid.Parse(business.Id),
                UserId = Guid.Parse(business.UserId),
                AccountType = Enum.Parse<AccountType>(business.AccountType),
                ImageUrl = business.ImageUrl,
                Name = business.Name,
                Description = business.Description
            },
            _ => throw new ArgumentException($"Unknown DTO type: {dto.GetType().Name}")
        };
    }
}
