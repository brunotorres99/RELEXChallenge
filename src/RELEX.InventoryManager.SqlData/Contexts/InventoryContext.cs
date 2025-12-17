using Microsoft.EntityFrameworkCore;
using RELEX.InventoryManager.SqlData.Configurations;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.SqlData.Contexts;

public class InventoryContext : DbContext, IInventoryContext
{
    public DbSet<OrderEntity> Orders { get; set; }

    public InventoryContext(DbContextOptions<InventoryContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("inv");

        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }

    public async Task SeedOrdersAsync(int seedNumber)
    {
        await Database.ExecuteSqlAsync($"call inv.SeedOrders ({seedNumber})");
    }
}