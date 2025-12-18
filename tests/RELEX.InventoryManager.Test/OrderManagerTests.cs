using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using FluentValidation;
using RELEX.InventoryManager.BusinessManager.Managers;
using RELEX.InventoryManager.BusinessManager.DTOs;
using RELEX.InventoryManager.BusinessManager.Validators;
using RELEX.InventoryManager.Common.Configutations;
using RELEX.InventoryManager.SqlData.Contexts;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.Test;

public class OrderManagerTests
{
    private static InventoryContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new InventoryContext(options);
    }

    private static IServiceProvider CreateServiceProviderForInMemory(string dbName)
    {
        var services = new ServiceCollection();
        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Register IInventoryContext so manager's SaveBatchToDatabase can resolve a new context that shares the same in-memory DB
        services.AddScoped<IInventoryContext>(_ => new InventoryContext(options));
        return services.BuildServiceProvider();
    }

    private static OrderManager CreateManager(string dbName, InventoryContext ctx)
    {
        var provider = CreateServiceProviderForInMemory(dbName);

        var inventoryOptions = Options.Create(new InventoryOptions
        {
            OrderProcessing = new OrderProcessingSettings
            {
                BatchSize = 1000
            }
        });

        return new OrderManager(
            NullLogger<OrderManager>.Instance,
            ctx,
            provider,
            new OrderDtoValidator(),
            new SearchOrderDtoValidator(),
            inventoryOptions);
    }

    private static OrderDto CreateValidOrderDto()
    {
        return new OrderDto
        {
            LocationCode = "Store-001",
            ProductCode = "prod-001",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Quantity = 5,
            SubmittedBy = "tester",
            SubmittedAt = DateTimeOffset.UtcNow
        };
    }

    private static async IAsyncEnumerable<OrderDto> ToAsyncEnumerable(params OrderDto[] items)
    {
        foreach (var it in items)
        {
            yield return it;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsNull_WhenNotFound);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var result = await manager.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOrder_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsOrder_WhenFound);
        using var ctx = CreateInMemoryContext(dbName);
        var entity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            LocationCode = "L1",
            ProductCode = "P1",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Quantity = 10,
            SubmittedBy = "user",
            SubmittedAt = DateTimeOffset.UtcNow
        };

        await ctx.Orders.AddAsync(entity);
        await ctx.SaveChangesAsync();

        var manager = CreateManager(dbName, ctx);

        var dto = await manager.GetByIdAsync(entity.Id);

        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto!.Id);
        Assert.Equal(entity.ProductCode, dto.ProductCode);
    }

    [Fact]
    public async Task CreateOrderAsync_Valid_AddsAndReturnsCreated()
    {
        var dbName = nameof(CreateOrderAsync_Valid_AddsAndReturnsCreated);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var order = CreateValidOrderDto();

        var created = await manager.CreateOrderAsync(order);

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Single(ctx.Orders);
        var stored = ctx.Orders.First();
        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(order.ProductCode, stored.ProductCode);
    }

    [Fact]
    public async Task CreateOrderAsync_Invalid_ThrowsValidationException()
    {
        var dbName = nameof(CreateOrderAsync_Invalid_ThrowsValidationException);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var invalid = CreateValidOrderDto();
        invalid.Quantity = 0; // invalid per validator

        await Assert.ThrowsAsync<ValidationException>(() => manager.CreateOrderAsync(invalid));
    }

    [Fact]
    public async Task UpdateOrderAsync_Throws_WhenNotFound()
    {
        var dbName = nameof(UpdateOrderAsync_Throws_WhenNotFound);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var order = CreateValidOrderDto();

        await Assert.ThrowsAsync<Exception>(() => manager.UpdateOrderAsync(Guid.NewGuid(), order));
    }

    [Fact]
    public async Task UpdateOrderAsync_UpdatesEntity_WhenFound()
    {
        var dbName = nameof(UpdateOrderAsync_UpdatesEntity_WhenFound);
        using var ctx = CreateInMemoryContext(dbName);
        var entity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            LocationCode = "L1",
            ProductCode = "P1",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Quantity = 10,
            SubmittedBy = "user",
            SubmittedAt = DateTimeOffset.UtcNow
        };

        await ctx.Orders.AddAsync(entity);
        await ctx.SaveChangesAsync();

        var manager = CreateManager(dbName, ctx);

        var update = new OrderDto
        {
            LocationCode = entity.LocationCode,
            ProductCode = "P1-modified",
            OrderDate = entity.OrderDate,
            Quantity = 20,
            SubmittedBy = entity.SubmittedBy,
            SubmittedAt = entity.SubmittedAt
        };

        var updated = await manager.UpdateOrderAsync(entity.Id, update);

        Assert.Equal(entity.Id, updated.Id);
        Assert.Equal("P1-modified", updated.ProductCode);

        var stored = ctx.Orders.First();
        Assert.Equal("P1-modified", stored.ProductCode);
        Assert.Equal(20, stored.Quantity);
    }

    [Fact]
    public async Task DeleteOrderAsync_RemovesEntity()
    {
        var dbName = nameof(DeleteOrderAsync_RemovesEntity);
        using var ctx = CreateInMemoryContext(dbName);
        var entity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            LocationCode = "L1",
            ProductCode = "P1",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Quantity = 10,
            SubmittedBy = "user",
            SubmittedAt = DateTimeOffset.UtcNow
        };

        await ctx.Orders.AddAsync(entity);
        await ctx.SaveChangesAsync();

        var manager = CreateManager(dbName, ctx);

        await manager.DeleteOrderAsync(entity.Id);

        Assert.Empty(ctx.Orders);
    }

    [Fact]
    public async Task CreateOrUpdateOrdersAsync_AddsNewOrdersFromStream()
    {
        var dbName = nameof(CreateOrUpdateOrdersAsync_AddsNewOrdersFromStream);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var dto = CreateValidOrderDto();
        // ensure Id is default (manager will assign new Id when saving)
        dto.Id = Guid.Empty;

        await manager.CreateOrUpdateOrdersAsync(ToAsyncEnumerable(dto));

        Assert.Single(ctx.Orders);
        var stored = ctx.Orders.First();
        Assert.Equal(dto.ProductCode, stored.ProductCode);
    }

    [Fact]
    public async Task CreateOrUpdateOrdersAsync_SkipsInvalidAndSavesValid()
    {
        var dbName = nameof(CreateOrUpdateOrdersAsync_SkipsInvalidAndSavesValid);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var valid = CreateValidOrderDto();
        valid.Id = Guid.Empty;

        var invalid = CreateValidOrderDto();
        invalid.Id = Guid.Empty;
        invalid.Quantity = 0; // invalid

        // Because batch size is large (1000) and we won't reach it, current implementation will skip invalid and save valid.
        await manager.CreateOrUpdateOrdersAsync(ToAsyncEnumerable(invalid, valid));

        var all = ctx.Orders.ToList();
        Assert.Single(all);
        Assert.Equal(valid.ProductCode, all[0].ProductCode);
    }

    [Fact]
    public async Task SearchOrdersAsync_FiltersAndAggregates()
    {
        var dbName = nameof(SearchOrdersAsync_FiltersAndAggregates);
        using var ctx = CreateInMemoryContext(dbName);
        // create orders across two dates and two products
        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var date2 = date1.AddDays(1);

        var e1 = new OrderEntity { Id = Guid.NewGuid(), LocationCode = "L1", ProductCode = "A", OrderDate = date1, Quantity = 5, SubmittedBy = "u", SubmittedAt = DateTimeOffset.UtcNow };
        var e2 = new OrderEntity { Id = Guid.NewGuid(), LocationCode = "L1", ProductCode = "A", OrderDate = date1, Quantity = 3, SubmittedBy = "u", SubmittedAt = DateTimeOffset.UtcNow };
        var e3 = new OrderEntity { Id = Guid.NewGuid(), LocationCode = "L1", ProductCode = "B", OrderDate = date2, Quantity = 7, SubmittedBy = "u", SubmittedAt = DateTimeOffset.UtcNow };

        await ctx.Orders.AddRangeAsync(e1, e2, e3);
        await ctx.SaveChangesAsync();

        var manager = CreateManager(dbName, ctx);

        var search = new SearchOrderDto
        {
            LocationCode = "L1",
            Aggregate = true,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await manager.SearchOrdersAsync(search);

        Assert.NotNull(result);
        Assert.NotNull(result.Aggregates);
        // Expect aggregates grouped by date/product; two aggregates (A on date1, B on date2)
        Assert.Equal(2, result.Aggregates!.Length);

        var aggA = result.Aggregates!.FirstOrDefault(a => a.ProductCode == "A");
        Assert.NotNull(aggA);
        Assert.Equal(2, aggA.Count);
        Assert.Equal(8, aggA.TotalQuantity);
    }

    [Fact]
    public async Task SearchOrdersStreamAsync_ReturnsStreamedResults()
    {
        var dbName = nameof(SearchOrdersStreamAsync_ReturnsStreamedResults);
        using var ctx = CreateInMemoryContext(dbName);
        var e1 = new OrderEntity { Id = Guid.NewGuid(), LocationCode = "L1", ProductCode = "X", OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date), Quantity = 1, SubmittedBy = "u", SubmittedAt = DateTimeOffset.UtcNow };
        var e2 = new OrderEntity { Id = Guid.NewGuid(), LocationCode = "L2", ProductCode = "X", OrderDate = DateOnly.FromDateTime(DateTime.UtcNow.Date), Quantity = 2, SubmittedBy = "u", SubmittedAt = DateTimeOffset.UtcNow };

        await ctx.Orders.AddRangeAsync(e1, e2);
        await ctx.SaveChangesAsync();

        var manager = CreateManager(dbName, ctx);

        var streamDto = new SearchOrderStreamDto { ProductCode = "X" };

        var list = new List<OrderDto>();
        await foreach (var o in manager.SearchOrdersStreamAsync(streamDto))
        {
            list.Add(o);
        }

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task SearchOrdersAsync_Throws_WhenInvalidSearchDto()
    {
        var dbName = nameof(SearchOrdersAsync_Throws_WhenInvalidSearchDto);
        using var ctx = CreateInMemoryContext(dbName);
        var manager = CreateManager(dbName, ctx);

        var bad = new SearchOrderDto
        {
            PageNumber = 1,
            PageSize = 10,
            OrderDateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2)),
            OrderDateTo = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        };

        await Assert.ThrowsAsync<ValidationException>(() => manager.SearchOrdersAsync(bad));
    }
}