using System.ComponentModel.DataAnnotations;
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
    [Required, StringLength(200)] string Name,
    [StringLength(1000)] string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === CUSTOMER ACCOUNT DTO ===
public record UpdateCustomerAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile,
    [StringLength(20)] string? PhoneNumber,
    [StringLength(100)] string? Name,
    [StringLength(100)] string? Surname,
    [StringLength(300)] string? Address
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);

// === COURIER ACCOUNT DTO ===
public record UpdateCourierAccountRequest(
    string? Id,
    string? UserId,
    AccountType AccountType,
    IFormFile? ImageFile,
    [StringLength(20)] string? PhoneNumber,
    [StringLength(100)] string? Name,
    [StringLength(100)] string? Surname,
    [StringLength(300)] string? Address,
    [StringLength(1000)] string? Description
) : UpdateAccountRequest(Id, UserId, AccountType, ImageFile);
