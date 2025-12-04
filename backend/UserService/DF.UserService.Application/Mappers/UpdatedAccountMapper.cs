using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class UpdatedAccountMapper
{
    public static Account ToEntity(UpdateAccountRequest request, Account existingAccount, string? imageUrl = null)
    {
        // Беремо існуючу сутність як основу, оновлюємо тільки те, що є у DTO
        switch (request)
        {
            case UpdateCustomerAccountRequest customer:
                var customerAccount = existingAccount as CustomerAccount 
                    ?? throw new ArgumentException("Invalid account type");
                customerAccount.Name = customer.Name ?? customerAccount.Name;
                customerAccount.Surname = customer.Surname ?? customerAccount.Surname;
                customerAccount.PhoneNumber = customer.PhoneNumber ?? customerAccount.PhoneNumber;
                customerAccount.Address = customer.Address ?? customerAccount.Address;
                if (imageUrl != null) customerAccount.ImageUrl = imageUrl;
                return customerAccount;

            case UpdateBusinessAccountRequest business:
                var businessAccount = existingAccount as BusinessAccount
                    ?? throw new ArgumentException("Invalid account type");
                businessAccount.Name = business.Name ?? businessAccount.Name;
                businessAccount.Description = business.Description ?? businessAccount.Description;
                if (imageUrl != null) businessAccount.ImageUrl = imageUrl;
                return businessAccount;

            case UpdateCourierAccountRequest courier:
                var courierAccount = existingAccount as CourierAccount
                    ?? throw new ArgumentException("Invalid account type");
                courierAccount.Name = courier.Name ?? courierAccount.Name;
                courierAccount.Surname = courier.Surname ?? courierAccount.Surname;
                courierAccount.PhoneNumber = courier.PhoneNumber ?? courierAccount.PhoneNumber;
                courierAccount.Address = courier.Address ?? courierAccount.Address;
                courierAccount.Description = courier.Description ?? courierAccount.Description;
                if (imageUrl != null) courierAccount.ImageUrl = imageUrl;
                return courierAccount;

            default:
                throw new ArgumentException($"Unknown DTO type: {request.GetType().Name}");
        }
    }
}