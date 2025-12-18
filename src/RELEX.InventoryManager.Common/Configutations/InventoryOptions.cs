namespace RELEX.InventoryManager.Common.Configutations;

public class InventoryOptions
{
    public DatabaseSettings? Database { get; set; }
    public OrderProcessingSettings? OrderProcessing { get; set; }
}