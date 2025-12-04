using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories;

public class AccountFactory(ICloudinaryService cloudinaryService) : IAccountFactory
{
    public async Task<Account> CreateAccount(CreateAccountRequest request, Guid userId)
    {
        UploadImageResult? image = null;
        if (request.ImageFile != null && request.ImageFile.Length > 0)
        {
            image = await cloudinaryService.UploadAsync(request.ImageFile,  "users");
        }
        return request switch
        {
            CreateCustomerAccountRequest c => new CustomerAccount
            {
                UserId = userId,
                AccountType = (AccountType)c.AccountType,
                Name = c.Name,
                Surname = c.Surname,
                Address = c.Address,
                PhoneNumber = c.PhoneNumber,
                ImageUrl = image?.Url ?? ""
            },
            CreateBusinessAccountRequest b => new BusinessAccount
            {
                UserId = userId,
                AccountType = (AccountType)b.AccountType,
                Name = b.Name,
                Description = b.Description,
                ImageUrl = image?.Url ?? ""
            },
            CreateCourierAccountRequest co => new CourierAccount
            {
                UserId = userId,
                AccountType = (AccountType)co.AccountType,
                Name = co.Name,
                Surname = co.Surname,
                Address = co.Address,
                PhoneNumber = co.PhoneNumber,
                Description = co.Description,
                ImageUrl = image?.Url ?? ""
            },
            _ => throw new ArgumentException("Unsupported AccountDTO type")
        };
    }
}
