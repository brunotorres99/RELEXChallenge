using Microsoft.EntityFrameworkCore;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.SqlData.Contexts;

public interface IInventoryContext : IDisposable
{
    public DbSet<OrderEntity> Orders { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    public Task SeedOrdersAsync(int seedNumber);
}