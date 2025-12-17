using FluentValidation;
using RELEX.InventoryManager.BusinessManager.DTOs;

namespace RELEX.InventoryManager.BusinessManager.Validators;

public class SearchOrderDtoValidator : AbstractValidator<SearchOrderDto>
{
    public SearchOrderDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 1000)
            .When(x => x.PageSize.HasValue);

        RuleFor(x => x)
            .Custom((dto, context) =>
            {
                if (dto.OrderDateFrom.HasValue && dto.OrderDateTo.HasValue && dto.OrderDateFrom > dto.OrderDateTo)
                {
                    context.AddFailure("OrderDateFrom", "OrderDateFrom must be less than or equal to OrderDateTo");
                }
            });
    }
}