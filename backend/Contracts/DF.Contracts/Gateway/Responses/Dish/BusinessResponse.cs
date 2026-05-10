using System;

namespace DF.Contracts.Gateway.Responses.Dish;

public record BusinessResponse(Guid Id, string Name, string Description);