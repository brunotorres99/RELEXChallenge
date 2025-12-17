using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RELEX.InventoryManager.SqlData.Contexts;

namespace RELEX.InventoryManager.SqlData;

public static class SqlDataServiceCollectionExtensions
{
    /// <summary>
    /// Register BusinessManagers.
    /// Call this from the composition root (API / host) after adding BusinessManager services.
    /// </summary>
    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, string connectionString)
    {
        if (connectionString == null) throw new ArgumentNullException(nameof(connectionString), "Database connectionString is missing.");

        services.AddDbContext<InventoryContext>(options =>
                options.UseNpgsql(connectionString));

        services.AddScoped<IInventoryContext, InventoryContext>();

        return services;
    }
}