using DF.UserService.Contracts.Models.DTO;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace DF.UserService.Contracts.Models.Request;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CustomerAccountResponse), "customer")]
[JsonDerivedType(typeof(BusinessAccountResponse), "business")]
[JsonDerivedType(typeof(CourierAccountResponse), "courier")]
// === BASE DTO ===
public abstract record UpdateAccountRequest(
    string? Id,
    string? UserId,
    int AccountType,
    IFormFile? ImageFile = null
);

// === CUSTOMER ACCOUNT DTO ===
public record UpdateCustomerAccountRequest(
    string? Id,
    string? UserId,
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === BUSINESS ACCOUNT DTO ===
public record UpdateBusinessAccountRequest(
    string? Id,
    string UserId,
    int AccountType,
    IFormFile? ImageFile,
    string Name,
    string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === COURIER ACCOUNT DTO ===
public record UpdateCourierAccountRequest(
    string? Id,
    string? UserId,
    int AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address,
    string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);
