using System;
using System.Collections.Generic;

namespace DF.Contracts.RPC.Requests.UserService;

public record GetAccountRequest(Guid? UserId);

public record GetCustomerAccountRequest(Guid CustomerId);
public record GetBusinessAccountRequest(Guid BusinessAccountId);
public record GetCourierAccountRequest(Guid CourierId);

public record GetCourierAccountsBatchRequest(List<Guid> CourierIds);
public record GetBusinessAccountsBatchRequest(List<Guid> BusinessIds);
public record GetCustomerAccountsBatchRequest(List<Guid> CustomerIds);