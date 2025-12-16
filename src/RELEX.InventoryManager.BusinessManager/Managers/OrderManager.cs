using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.DTOs;
using RELEX.InventoryManager.BusinessManager.Mappers;
using RELEX.InventoryManager.SqlData.Contexts;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.BusinessManager.Managers;

public class OrderManager(ILogger<OrderManager> logger, IInventoryContext context) : IOrderManager
{
    private readonly ILogger<OrderManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IInventoryContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<OrderDto?> GetByIdAsync(Guid orderId)
    {
        OrderEntity? order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null) return null;

        return order.ToDto();
    }

    public async Task<SearchOrderResultDto> SearchOrdersAsync(SearchOrderDto searchOrderDto)
    {
        IQueryable<OrderEntity> query = _context.Orders.AsQueryable().AsNoTracking();

        // apply filters
        if (string.IsNullOrWhiteSpace(searchOrderDto?.LocationCode) == false)
            query = query.Where(x => x.LocationCode == searchOrderDto.LocationCode);

        if (string.IsNullOrWhiteSpace(searchOrderDto?.ProductCode) == false)
            query = query.Where(x => x.ProductCode == searchOrderDto.ProductCode);

        if (searchOrderDto?.OrderDateFrom is not null)
            query = query.Where(x => x.OrderDate >= searchOrderDto.OrderDateFrom);

        if (searchOrderDto?.OrderDateTo is not null)
            query = query.Where(x => x.OrderDate <= searchOrderDto.OrderDateTo);

        SearchOrderResultDto searchResult = new ();

        // set agreegates
        if (searchOrderDto!.Aggregate is true)
        {
            searchResult.Aggregates = await query
                .GroupBy(x => new { x.OrderDate, x.ProductCode })
                .Select(x => new OrderAggregateDto
                {
                    OrderDate = x.Key.OrderDate,
                    ProductCode = x.Key.ProductCode,
                    Count = x.Count(),
                    TotalQuantity = x.Sum(x => x.Quantity),
                    AverageQuantity = Math.Round(x.Average(x => x.Quantity), 2)
                })
                .OrderBy(x => x.OrderDate)
                .ThenBy(x => x.ProductCode)
                .ToArrayAsync();
        }

        // set orders with pagination
        query = query.OrderBy(x => x.OrderDate);
        query = query.Skip((searchOrderDto!.PageNumber!.Value - 1) * searchOrderDto.PageSize!.Value).Take(searchOrderDto.PageSize!.Value);

        // map to dto
        searchResult.Orders = await query.Select(order => order.ToDto()).ToArrayAsync();

        return searchResult;
    }

    public async Task<OrderDto> CreateOrderAsync(OrderDto orderDto)
    {
        OrderEntity? orderEntity = orderDto.ToEntity();

        await _context.Orders.AddAsync(orderEntity);
        await _context.SaveChangesAsync();

        return orderEntity.ToDto();
    }

    public async Task<OrderDto> UpdateOrderAsync(Guid id, OrderDto orderDto)
    {
        OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

        if (orderEntity is null) throw new Exception("Order not found");

        OrderMapper.UpdateEntity(orderDto, orderEntity);

        await _context.SaveChangesAsync();

        return orderEntity.ToDto();
    }

    public async Task DeleteOrderAsync(Guid id)
    {
        OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

        if (orderEntity is null) throw new Exception("Order not found");

        _context.Orders.Remove(orderEntity);
        await _context.SaveChangesAsync();
    }

    public async Task CreateOrUpdateOrdersAsync(IAsyncEnumerable<OrderDto> batchOrders)
    {
        var batch = new List<OrderDto>();
        const int batchSize = 1000;

        await foreach (OrderDto orderDto in batchOrders)
        {
            batch.Add(orderDto);
            if (batch.Count >= batchSize)
            {
                await SaveBatchToDatabase(batch);
                batch.Clear();
            }
        }

        // Save remaining items
        if (batch.Count > 0)
        {
            await SaveBatchToDatabase(batch);
        }
    }

    private async Task SaveBatchToDatabase(List<OrderDto> batchOrders)
    {
        foreach (OrderDto orderDto in batchOrders)
        {
            OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderDto.Id);

            if (orderEntity is null)
            {
                await _context.Orders.AddAsync(orderDto.ToEntity());
            }
            else
            {
                OrderMapper.UpdateEntity(orderDto, orderEntity);
            }
        }

        await _context.SaveChangesAsync();
    }
}