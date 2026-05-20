using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories;

public class AccountFactory : IAccountFactory
{
    public Account CreateAccount(CreateAccountRequest request, Guid userId, UploadImageResult? image)
    {
        var imageUrl = image?.Url ?? string.Empty;
        var imagePublicId = image?.PublicId;

        return request switch
        {
            CreateCustomerAccountRequest c => new CustomerAccount
            {
                UserId = userId,
                AccountType = (AccountType)c.AccountType,
                Name = c.Name ?? string.Empty,
                Surname = c.Surname ?? string.Empty,
                Address = c.Address,
                PhoneNumber = c.PhoneNumber,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId
            },
            CreateBusinessAccountRequest b => new BusinessAccount
            {
                UserId = userId,
                AccountType = (AccountType)b.AccountType,
                Name = b.Name,
                Description = b.Description,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId
            },
            CreateCourierAccountRequest co => new CourierAccount
            {
                UserId = userId,
                AccountType = (AccountType)co.AccountType,
                Name = co.Name ?? string.Empty,
                Surname = co.Surname ?? string.Empty,
                Address = co.Address,
                PhoneNumber = co.PhoneNumber,
                Description = co.Description,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId
            },
            _ => throw new ArgumentException("Unsupported AccountDTO type")
        };
    }
}
