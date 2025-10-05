namespace DF.UserService.Contracts.Models.DTO
{
    // === BASE DTO ===
    public abstract record AccountDTO(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl = null
    );

    // === CUSTOMER ACCOUNT DTO ===
    public record CustomerAccountDTO(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl,
        string? PhoneNumber,
        string? Name,
        string? Surname,
        string? Address
    ) : AccountDTO(Id, UserId, AccountType, ImageUrl);

    // === BUSINESS ACCOUNT DTO ===
    public record BusinessAccountDTO(
        string? Id,
        string UserId,
        string AccountType,
        string? ImageUrl,
        string Name,
        string? Description
    ) : AccountDTO(Id, UserId, AccountType, ImageUrl);

    // === COURIER ACCOUNT DTO ===
    public record CourierAccountDTO(
        string? Id,
        string? UserId,
        string? AccountType,
        string? ImageUrl,
        string? PhoneNumber,
        string? Name,
        string? Surname,
        string? Address,
        string? Description
    ) : AccountDTO(Id, UserId, AccountType, ImageUrl);



    //TODO: create DTO for accounts
}
