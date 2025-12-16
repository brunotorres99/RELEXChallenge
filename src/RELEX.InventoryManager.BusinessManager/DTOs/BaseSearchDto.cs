namespace RELEX.InventoryManager.BusinessManager.DTOs;

public record BaseSearchDto
{
    public int? PageNumber { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
}