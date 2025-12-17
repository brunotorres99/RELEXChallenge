using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.DTOs;
using RELEX.InventoryManager.BusinessManager.Managers;
using RELEX.InventoryManager.BusinessManager.Validators;

namespace RELEX.InventoryManager.BusinessManager;

public static class BusinessManagerServiceCollectionExtensions
{
    /// <summary>
    /// Register BusinessManagers.
    /// Call this from the composition root (API / host) after adding BusinessManager services.
    /// </summary>
    public static IServiceCollection AddBusinessManagers(this IServiceCollection services)
    {
        services.AddScoped<IOrderManager, OrderManager>();

        return services;
    }

    /// <summary>
    /// Register BusinessManager validators.
    /// Call this from the composition root (API / host) after adding BusinessManager services.
    /// </summary>
    public static IServiceCollection AddBusinessManagerValidators(this IServiceCollection services)
    {
        services.AddTransient<IValidator<OrderDto>, OrderDtoValidator>();
        services.AddTransient<IValidator<SearchOrderDto>, SearchOrderDtoValidator>();

        return services;
    }
}