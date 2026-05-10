using System;

namespace DF.Contracts.Gateway.Requests.Dish;

public record CreateIngredientRequest(string Name, int Weight);

public record UpdateIngredientRequest(Guid? Id, string Name, int Weight);