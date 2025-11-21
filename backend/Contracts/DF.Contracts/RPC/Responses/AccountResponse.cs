namespace DF.Contracts.RPC.Responses;

public record GetAccountResponse(Guid AccountId, Guid UserId, string AccountType);
