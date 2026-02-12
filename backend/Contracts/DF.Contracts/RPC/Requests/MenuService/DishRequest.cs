using System;

namespace DF.Contracts.RPC.Requests.MenuService;

public record GetDishesRequest(Guid BusinessId);
public record GetDishRequest(Guid DishId);