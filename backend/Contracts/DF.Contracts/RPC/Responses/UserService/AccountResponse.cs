using System;
using System.Collections.Generic;

namespace DF.Contracts.RPC.Responses.UserService;

public record GetAccountResponse(Guid AccountId, Guid UserId, string AccountType);

public record GetCustomerAccountResponse(
    Guid AccountId,
    Guid UserId,
    string AccountType,
    string ImageUrl,
    string Name,
    string Surname,
    string PhoneNumber,
    string Email,
    string Address
    ) : GetAccountResponse(AccountId, UserId, AccountType);
public record GetBusinessAccountResponse(
    Guid AccountId,
    Guid UserId,
    string AccountType,
    string ImageUrl,
    string Name,
    string Description,
    string PhoneNumber,
    List<string> Adresses 
    ) : GetAccountResponse(AccountId, UserId, AccountType);
public record GetCourierAccountResponse(
    Guid AccountId,
    Guid UserId,
    string AccountType,
    string ImageUrl,
    string Name,
    string Surname,
    string PhoneNumber,
    string Email,
    string Address
    )  : GetAccountResponse(AccountId, UserId, AccountType);
