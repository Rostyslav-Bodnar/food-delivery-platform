using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountMapper
{
    // === ENTITY → DTO ===
    
    public static AccountResponse ToDTO(Account account)
    {
        return account switch
        {
            CourierAccount courier => new CourierAccountResponse
            {
                Id = courier.Id.ToString(),
                UserId = courier.UserId.ToString(),
                AccountType = courier.AccountType.ToString(),
                ImageUrl = courier.ImageUrl,
                PhoneNumber = courier.PhoneNumber,
                Name = courier.Name,
                Surname = courier.Surname,
                Address = courier.Address,
                Description = courier.Description
            },

            CustomerAccount customer => new CustomerAccountResponse
            {
                Id = customer.Id.ToString(),
                UserId = customer.UserId.ToString(),
                AccountType = customer.AccountType.ToString(),
                ImageUrl = customer.ImageUrl,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                Surname = customer.Surname,
                Address = customer.Address
            },

            BusinessAccount business => new BusinessAccountResponse
            {
                Id = business.Id.ToString(),
                UserId = business.UserId.ToString(),
                AccountType = business.AccountType.ToString(),
                ImageUrl = business.ImageUrl,
                Name = business.Name,
                Description = business.Description,
                StripeAccountId = business.StripeAccountId,
                StripeChargesEnabled = business.StripeChargesEnabled,
                StripePayoutsEnabled = business.StripePayoutsEnabled,
                StripeRequirementsDue = business.StripeRequirementsDue,
                StripeOnboardedAt = business.StripeOnboardedAt
            },

            _ => throw new ArgumentException(
                $"Unknown account type: {account.GetType().Name}"
            )
        };
    }


    // === DTO → ENTITY ===
    public static Account ToEntity(CreateAccountRequest request, Guid userId, string? imageUrl = null)
    {
        return request switch
        {
            CreateCourierAccountRequest courier => new CourierAccount
            {
                UserId = userId,
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
                UserId = userId,
                AccountType = (AccountType)customer.AccountType,
                ImageUrl = imageUrl,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                Surname = customer.Surname,
                Address = customer.Address
            },
            CreateBusinessAccountRequest business => new BusinessAccount
            {
                UserId = userId,
                AccountType = (AccountType)business.AccountType,
                ImageUrl = imageUrl,
                Name = business.Name,
                Description = business.Description
            },
            _ => throw new ArgumentException($"Unknown DTO type: {request.GetType().Name}")
        };
    }
}
