using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountMapper
{
    // === ENTITY → DTO ===
    public static AccountResponse ToDTO(Account account)
    {
        return account switch
        {
            CourierAccount courier => new CourierAccountResponse(
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
            CustomerAccount customer => new CustomerAccountResponse(
                customer.Id.ToString(),
                customer.UserId.ToString(),
                customer.AccountType.ToString(),
                customer.ImageUrl,
                customer.PhoneNumber,
                customer.Name,
                customer.Surname,
                customer.Address
            ),
            BusinessAccount business => new BusinessAccountResponse(
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
    public static Account ToEntity(CreateAccountRequest request, string? imageUrl = null)
    {
        return request switch
        {
            CreateCourierAccountRequest courier => new CourierAccount
            {
                UserId = Guid.Parse(courier.UserId),
                AccountType = (AccountType)courier.AccountType,
                ImageUrl = imageUrl,
                PhoneNumber = courier.PhoneNumber,
                Name = courier.Name,
                Surname = courier.Surname,
                Address = courier.Address,
                Description = courier.Description
            },
            CreateCustomerAccountRequest customer => new CustomerAccount
            {
                UserId = Guid.Parse(customer.UserId),
                AccountType = (AccountType)customer.AccountType,
                ImageUrl = imageUrl,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                Surname = customer.Surname,
                Address = customer.Address
            },
            CreateBusinessAccountRequest business => new BusinessAccount
            {
                UserId = Guid.Parse(business.UserId),
                AccountType = (AccountType)business.AccountType,
                ImageUrl = imageUrl,
                Name = business.Name,
                Description = business.Description
            },
            _ => throw new ArgumentException($"Unknown DTO type: {request.GetType().Name}")
        };
    }
}
