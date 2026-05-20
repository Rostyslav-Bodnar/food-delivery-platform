using System;
using System.Collections.Generic;

namespace DF.Contracts.RPC.Requests.MenuService;

public record GetDishesRequest(Guid BusinessId);
public record GetDishRequest(Guid DishId);
public record GetDishesBatchRequest(List<Guid> DishIds);