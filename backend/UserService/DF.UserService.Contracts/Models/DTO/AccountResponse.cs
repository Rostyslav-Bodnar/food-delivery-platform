using System.Text.Json.Serialization;

namespace DF.UserService.Contracts.Models.DTO
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(CustomerAccountResponse), "customer")]
    [JsonDerivedType(typeof(BusinessAccountResponse), "business")]
    [JsonDerivedType(typeof(CourierAccountResponse), "courier")]
    // === BASE DTO ===
    public abstract record AccountResponse(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl = null
    );

    // === CUSTOMER ACCOUNT DTO ===
    public record CustomerAccountResponse(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl,
        string? PhoneNumber,
        string? Name,
        string? Surname,
        string? Address
    ) : AccountResponse(Id, UserId, AccountType, ImageUrl);

    // === BUSINESS ACCOUNT DTO ===
    public record BusinessAccountResponse(
        string? Id,
        string UserId,
        string AccountType,
        string? ImageUrl,
        string Name,
        string? Description
    ) : AccountResponse(Id, UserId, AccountType, ImageUrl);

    // === COURIER ACCOUNT DTO ===
    public record CourierAccountResponse(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl,
        string? PhoneNumber,
        string? Name,
        string? Surname,
        string? Address,
        string? Description
    ) : AccountResponse(Id, UserId, AccountType, ImageUrl);



    //TODO: create DTO for accounts
}
