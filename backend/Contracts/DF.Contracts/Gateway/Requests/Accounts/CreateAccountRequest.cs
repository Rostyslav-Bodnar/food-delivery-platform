using System.Text.Json.Serialization;
using DF.Contracts.Enums;
using Microsoft.AspNetCore.Http;

namespace DF.Contracts.Gateway.Requests.Accounts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CreateCustomerAccountRequest), "customer")]
[JsonDerivedType(typeof(CreateBusinessAccountRequest), "business")]
[JsonDerivedType(typeof(CreateCourierAccountRequest), "courier")]
public abstract record CreateAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile = null
);


public record CreateCustomerAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address
) : CreateAccountRequest(AccountType, ImageFile);


public record CreateBusinessAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile,
    string Name,
    string? Description
) : CreateAccountRequest(AccountType, ImageFile);


public record CreateCourierAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address,
    string? Description
) : CreateAccountRequest(AccountType, ImageFile);
