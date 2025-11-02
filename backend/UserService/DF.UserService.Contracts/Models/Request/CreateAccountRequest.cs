using System.Text.Json.Serialization;
using DF.UserService.Contracts.Models.DTO;
using Microsoft.AspNetCore.Http;

namespace DF.UserService.Contracts.Models.Request;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CustomerAccountResponse), "customer")]
[JsonDerivedType(typeof(BusinessAccountResponse), "business")]
[JsonDerivedType(typeof(CourierAccountResponse), "courier")]
// === BASE DTO ===
public abstract record CreateAccountRequest(
    int AccountType,
    IFormFile? ImageFile = null
);

// === CUSTOMER ACCOUNT DTO ===
public record CreateCustomerAccountRequest(
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address
) : CreateAccountRequest(AccountType, ImageFile);

// === BUSINESS ACCOUNT DTO ===
public record CreateBusinessAccountRequest(
    int AccountType,
    IFormFile? ImageFile,
    string? Name,
    string? Description
) : CreateAccountRequest(AccountType, ImageFile);

// === COURIER ACCOUNT DTO ===
public record CreateCourierAccountRequest(
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address,
    string? Description
) : CreateAccountRequest(AccountType, ImageFile);
