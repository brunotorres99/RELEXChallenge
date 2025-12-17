namespace RELEX.InventoryManager.BusinessManager.DTOs;

public record SearchOrderDto :  BaseSearchDto
{
    public string? LocationCode { get; set; }
    public string? ProductCode { get; set; }

    public DateOnly? OrderDateFrom { get; set; }
    public DateOnly? OrderDateTo { get; set; }

    public bool? Aggregate { get; set; } = false;
}

public record SearchOrderStreamDto
{
    public string? LocationCode { get; set; }
    public string? ProductCode { get; set; }

    public DateOnly? OrderDateFrom { get; set; }
    public DateOnly? OrderDateTo { get; set; }
}