namespace DF.Contracts.RPC.Requests.UserService;

public record GetAccountRequest(Guid? UserId);

public record GetCustomerAccountRequest(Guid CustomerId);
public record GetBusinessAccountRequest(Guid BusinessAccountId);
public record GetCourierAccountRequest(Guid CourierId);