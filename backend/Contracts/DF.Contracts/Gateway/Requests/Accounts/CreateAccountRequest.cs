using System.ComponentModel.DataAnnotations;
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
    [StringLength(20)] string? PhoneNumber,
    [StringLength(100)] string? Name,
    [StringLength(100)] string? Surname,
    [StringLength(300)] string? Address
) : CreateAccountRequest(AccountType, ImageFile);


public record CreateBusinessAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile,
    [Required, StringLength(200)] string Name,
    [StringLength(1000)] string? Description
) : CreateAccountRequest(AccountType, ImageFile);


public record CreateCourierAccountRequest(
    AccountType AccountType,
    IFormFile? ImageFile,
    [StringLength(20)] string? PhoneNumber,
    [StringLength(100)] string? Name,
    [StringLength(100)] string? Surname,
    [StringLength(300)] string? Address,
    [StringLength(1000)] string? Description
) : CreateAccountRequest(AccountType, ImageFile);
