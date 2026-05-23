using DF.Contracts.Gateway.Requests.Dish;
using FluentValidation;

namespace DF.MenuService.Application.Validation;

public class CreateIngredientRequestValidator : AbstractValidator<CreateIngredientRequest>
{
    public CreateIngredientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Weight).InclusiveBetween(1, 100_000);
    }
}

public class UpdateIngredientRequestValidator : AbstractValidator<UpdateIngredientRequest>
{
    public UpdateIngredientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Weight).InclusiveBetween(1, 100_000);
    }
}

public class CreateDishRequestValidator : AbstractValidator<CreateDishRequest>
{
    private const long MaxImageBytes = 5L * 1024 * 1024;

    public CreateDishRequestValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.CookingTime).InclusiveBetween(0, 600);
        RuleFor(x => x.Ingredients).NotNull();
        RuleForEach(x => x.Ingredients).SetValidator(new CreateIngredientRequestValidator());
        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.Image!.Length).LessThanOrEqualTo(MaxImageBytes)
                .WithMessage($"Image must be <= {MaxImageBytes / (1024 * 1024)} MB");
        });
    }
}

public class UpdateDishRequestValidator : AbstractValidator<UpdateDishRequest>
{
    private const long MaxImageBytes = 5L * 1024 * 1024;

    public UpdateDishRequestValidator()
    {
        RuleFor(x => x.DishId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.CookingTime).InclusiveBetween(0, 600);
        RuleFor(x => x.Ingredients).NotNull();
        RuleForEach(x => x.Ingredients).SetValidator(new UpdateIngredientRequestValidator());
        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.Image!.Length).LessThanOrEqualTo(MaxImageBytes)
                .WithMessage($"Image must be <= {MaxImageBytes / (1024 * 1024)} MB");
        });
    }
}
