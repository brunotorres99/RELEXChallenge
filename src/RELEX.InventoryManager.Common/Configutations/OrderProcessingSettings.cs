namespace RELEX.InventoryManager.Common.Configutations;

public class OrderProcessingSettings
{
    /// <summary>
    /// Number of items to process per batch when upserting orders.
    /// Default: 1000.
    /// Must be > 0.
    /// </summary>
    public int? BatchSize { get; set; } = 1000;
}