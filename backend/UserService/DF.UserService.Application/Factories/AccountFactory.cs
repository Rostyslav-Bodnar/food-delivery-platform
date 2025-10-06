using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories;

public class AccountFactory : IAccountFactory
{
    public Account CreateAccount(AccountDTO dto)
    {
        return dto switch
        {
            CustomerAccountDTO c => new CustomerAccount
            {
                Id = string.IsNullOrEmpty(c.Id) ? Guid.NewGuid() : Guid.Parse(c.Id),
                UserId = Guid.Parse(c.UserId),
                AccountType = Enum.Parse<AccountType>(c.AccountType),
                Name = c.Name,
                Surname = c.Surname,
                Address = c.Address,
                PhoneNumber = c.PhoneNumber,
                ImageUrl = c.ImageUrl
            },
            BusinessAccountDTO b => new BusinessAccount
            {
                Id = string.IsNullOrEmpty(b.Id) ? Guid.NewGuid() : Guid.Parse(b.Id),
                UserId = Guid.Parse(b.UserId),
                AccountType = Enum.Parse<AccountType>(b.AccountType),
                Name = b.Name,
                Description = b.Description,
                ImageUrl = b.ImageUrl
            },
            CourierAccountDTO co => new CourierAccount
            {
                Id = string.IsNullOrEmpty(co.Id) ? Guid.NewGuid() : Guid.Parse(co.Id),
                UserId = Guid.Parse(co.UserId),
                AccountType = Enum.Parse<AccountType>(co.AccountType),
                Name = co.Name,
                Surname = co.Surname,
                Address = co.Address,
                PhoneNumber = co.PhoneNumber,
                Description = co.Description,
                ImageUrl = co.ImageUrl
            },
            _ => throw new ArgumentException("Unsupported AccountDTO type")
        };
    }
}
