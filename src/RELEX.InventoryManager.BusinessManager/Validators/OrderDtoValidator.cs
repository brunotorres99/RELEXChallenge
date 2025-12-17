using FluentValidation;
using RELEX.InventoryManager.BusinessManager.DTOs;

namespace RELEX.InventoryManager.BusinessManager.Validators;

public class OrderDtoValidator : AbstractValidator<OrderDto>
{
    public OrderDtoValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ProductCode)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.SubmittedBy)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.OrderDate)
            .Must(d => d > DateOnly.MinValue)
            .WithMessage("OrderDate must be set");

        RuleFor(x => x.SubmittedAt)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("SubmittedAt must be set");
    }
}