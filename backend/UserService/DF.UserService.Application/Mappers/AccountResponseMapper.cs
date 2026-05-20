using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountResponseMapper
{
    public static GetAccountResponse ToBaseResponse(Account account)
    {
        return new GetAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString());
    }

    public static GetCourierAccountResponse ToCourierResponse(
        CourierAccount account,
        string? email)
    {
        return new GetCourierAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString(),
            account.ImageUrl ?? string.Empty,
            account.Name,
            account.Surname,
            account.PhoneNumber ?? string.Empty,
            email ?? string.Empty,
            account.Address ?? string.Empty);
    }

    public static GetCustomerAccountResponse ToCustomerResponse(
        CustomerAccount account,
        string? email)
    {
        return new GetCustomerAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString(),
            account.ImageUrl ?? string.Empty,
            account.Name,
            account.Surname,
            account.PhoneNumber ?? string.Empty,
            email ?? string.Empty,
            account.Address ?? string.Empty);
    }

    public static GetBusinessAccountResponse ToBusinessResponse(
        BusinessAccount account)
    {
        return new GetBusinessAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString(),
            account.ImageUrl ?? string.Empty,
            account.Name,
            account.Description ?? string.Empty,
            string.Empty,
            [],
            account.StripeChargesEnabled ?? false,
            account.StripePayoutsEnabled ?? false,
            account.StripeRequirementsDue ?? string.Empty,
            account.StripeAccountId ?? string.Empty);
    }
}