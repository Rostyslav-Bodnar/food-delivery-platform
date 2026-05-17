using DF.Contracts.Gateway.Requests.Order;
using FluentValidation;

namespace DF.OrderService.Application.Validation;

public class CreateOrderDishRequestValidator : AbstractValidator<CreateOrderDishRequest>
{
    public CreateOrderDishRequestValidator()
    {
        RuleFor(x => x.DishId).NotEmpty();
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.OrderedBy).NotEmpty();
        RuleFor(x => x.OrderDate)
            .Must(d => d <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("OrderDate cannot be in the future");
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0); // server recomputes — client value is advisory
        RuleFor(x => x.Dishes).NotNull().NotEmpty();
        RuleForEach(x => x.Dishes).SetValidator(new CreateOrderDishRequestValidator());
        RuleFor(x => x.DeliverTo).NotNull();
        RuleFor(x => x.DeliverFrom).NotNull();
    }
}
