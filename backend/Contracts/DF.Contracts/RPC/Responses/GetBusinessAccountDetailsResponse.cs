namespace DF.Contracts.RPC.Responses;

public record GetBusinessAccountDetailsResponse(Guid BusinessAccountId, Guid UserId, string Name, string Description); //TODO: need to add location