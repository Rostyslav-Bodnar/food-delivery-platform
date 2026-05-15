using System.Text.Json.Serialization;
using DF.Contracts.Enums;
using Microsoft.AspNetCore.Http;

namespace DF.Contracts.Gateway.Requests.Accounts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(UpdateCustomerAccountRequest), "customer")]
[JsonDerivedType(typeof(UpdateBusinessAccountRequest), "business")]
[JsonDerivedType(typeof(UpdateCourierAccountRequest), "courier")]
public abstract record UpdateAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile = null
);

public record UpdateBusinessAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile,
    string Name,
    string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === CUSTOMER ACCOUNT DTO ===
public record UpdateCustomerAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === COURIER ACCOUNT DTO ===
public record UpdateCourierAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile,
    string? PhoneNumber,
    string? Name,
    string? Surname,
    string? Address,
    string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

