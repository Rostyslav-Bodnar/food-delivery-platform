using System;

namespace DF.Contracts.Gateway.Responses.Dish;

public record IngredientResponse(Guid Id, Guid DishId,  string Name, int Weight);