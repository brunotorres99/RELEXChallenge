namespace RELEX.InventoryManager.SqlData.Entities;

public class OrderEntity
{
    public Guid Id { get; set; }
    public required string LocationCode { get; set; }
    public required string ProductCode { get; set; }
    public DateOnly OrderDate { get; set; }
    public int Quantity { get; set; }
    public required string SubmittedBy { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}