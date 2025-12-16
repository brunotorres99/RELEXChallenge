namespace RELEX.InventoryManager.BusinessManager.DTOs;

public record SearchOrderResultDto
{
    public OrderDto[]? Orders { get; set; }
    public OrderAggregateDto[]? Aggregates { get; set; }
}

public record OrderAggregateDto
{
    public DateOnly OrderDate { get; set; }
    public string ProductCode { get; set; }
    public int Count { get; set; }
    public int TotalQuantity { get; set; }
    public double AverageQuantity { get; set; }
}