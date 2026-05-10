using System;
using System.Collections.Generic;

namespace DF.Contracts.Gateway.Responses;

public record ProfileResponse(
    UserDto User,
    AccountResponse CurrentAccount,
    IEnumerable<AccountResponse> Accounts);

public record UserDto(
    Guid Id,
    string Email,
    string Name, 
    string Surname, 
    string UserRole, 
    AccountResponse CurrentAccount);
