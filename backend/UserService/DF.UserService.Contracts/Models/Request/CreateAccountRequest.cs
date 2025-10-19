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
    string? UserId,
    int AccountType,
    IFormFile? ImageFile = null
);

// === CUSTOMER ACCOUNT DTO ===
public record CreateCustomerAccountRequest(
    string? UserId,
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address
) : CreateAccountRequest(UserId, AccountType, ImageFile);

// === BUSINESS ACCOUNT DTO ===
public record CreateBusinessAccountRequest(
    string UserId,
    int AccountType,
    IFormFile? ImageFile,
    string Name,
    string? Description
) : CreateAccountRequest(UserId, AccountType, ImageFile);

// === COURIER ACCOUNT DTO ===
public record CreateCourierAccountRequest(
    string? UserId,
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address,
    string? Description
) : CreateAccountRequest(UserId, AccountType, ImageFile);
