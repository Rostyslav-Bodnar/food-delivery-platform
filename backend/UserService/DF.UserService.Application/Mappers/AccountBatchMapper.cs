using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Mappers;

public static class AccountBatchMapper
{
    public static List<GetBusinessAccountResponse> ToBusinessList(List<BusinessAccount> accounts)
        => accounts.Select(AccountResponseMapper.ToBusinessResponse).ToList();

    public static List<GetCourierAccountResponse> ToCourierList(
        List<CourierAccount> accounts,
        Dictionary<Guid, string?> emails)
    {
        return accounts.Select(a =>
            AccountResponseMapper.ToCourierResponse(
                a,
                emails.GetValueOrDefault(a.UserId)
            )
        ).ToList();
    }
    
    public static List<GetCustomerAccountResponse> ToCustomerList(
        List<CustomerAccount> accounts,
        Dictionary<Guid, string?> emails)
    {
        return accounts.Select(a =>
            AccountResponseMapper.ToCustomerResponse(
                a,
                emails.GetValueOrDefault(a.UserId)
            )
        ).ToList();
    }
}