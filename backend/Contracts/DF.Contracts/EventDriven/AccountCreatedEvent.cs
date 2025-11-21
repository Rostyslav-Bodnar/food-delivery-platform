namespace DF.Contracts.EventDriven;

public record AccountCreatedEvent(
    Guid AccountId,
    Guid UserId,
    string AccountType,
    DateTime CreatedAt
);