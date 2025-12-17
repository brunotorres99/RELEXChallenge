using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.DTOs;
using RELEX.InventoryManager.BusinessManager.Mappers;
using RELEX.InventoryManager.SqlData.Contexts;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.BusinessManager.Managers;

/// <summary>
/// Manager responsible for order lifecycle operations: querying, creating, updating, deleting,
/// and batch upsert of orders.
/// 
/// Responsibilities:
/// - Validate input DTOs using provided FluentValidation validators.
/// - Translate between DTOs and EF Core entities via mappers.
/// - Persist changes via <see cref="IInventoryContext"/>.
///
/// Important notes:
/// - The underlying <see cref="IInventoryContext"/> (EF Core DbContext) is NOT thread-safe. Callers
///   must not attempt to enumerate streaming results concurrently with other operations on the same context.
/// - Methods perform optimistic, single-operation saves. Transactional coordination across external systems
///   is out of scope for this manager and should be handled by higher-level orchestration if required.
/// </summary>
public class OrderManager(ILogger<OrderManager> logger,
                         IInventoryContext context,
                         IValidator<OrderDto> orderValidator,
                         IValidator<SearchOrderDto> searchOrderValidator) : IOrderManager
{
    private readonly ILogger<OrderManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IInventoryContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IValidator<OrderDto> _orderValidator = orderValidator ?? throw new ArgumentNullException(nameof(orderValidator));
    private readonly IValidator<SearchOrderDto> _searchOrderValidator = searchOrderValidator ?? throw new ArgumentNullException(nameof(searchOrderValidator));

    /// <summary>
    /// Retrieves an order by its identifier.
    /// </summary>
    /// <param name="orderId">Unique identifier of the order to retrieve.</param>
    /// <returns>
    /// The matching <see cref="OrderDto"/> if found; otherwise <c>null</c>.
    /// </returns>
    public async Task<OrderDto?> GetByIdAsync(Guid orderId)
    {
        OrderEntity? order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null) return null;

        return order.ToDto();
    }

    /// <summary>
    /// Searches orders applying filters, optional aggregation and pagination.
    /// </summary>
    /// <param name="searchOrderDto">Search criteria and pagination parameters. Must be non-null and valid.</param>
    /// <returns>A <see cref="SearchOrderResultDto"/> containing matching orders and optional aggregates.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="searchOrderDto"/> is null.</exception>
    /// <exception cref="FluentValidation.ValidationException">Thrown when validation of <paramref name="searchOrderDto"/> fails.</exception>
    public async Task<SearchOrderResultDto> SearchOrdersAsync(SearchOrderDto searchOrderDto)
    {
        if (searchOrderDto is null) throw new ArgumentNullException(nameof(searchOrderDto));

        var validation = await _searchOrderValidator.ValidateAsync(searchOrderDto);
        if (!validation.IsValid) throw new FluentValidation.ValidationException(validation.Errors);

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

        SearchOrderResultDto searchResult = new();

        // set aggregates if requested
        if (searchOrderDto!.Aggregate is true)
        {
            // Group by date and product and compute aggregate metrics.
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

        // apply ordering and pagination for the orders result set
        query = query.OrderBy(x => x.OrderDate);
        query = query.Skip((searchOrderDto!.PageNumber!.Value - 1) * searchOrderDto.PageSize!.Value).Take(searchOrderDto.PageSize!.Value);

        // map to DTOs
        searchResult.Orders = await query.Select(order => order.ToDto()).ToArrayAsync();

        return searchResult;
    }

    /// <summary>
    /// Streams matching orders as an async enumerable. Useful for large result sets to avoid buffering.
    /// </summary>
    /// <param name="searchOrderDto">Streaming search criteria. Must be non-null.</param>
    /// <returns>An async stream of <see cref="OrderDto"/>.</returns>
    /// <remarks>
    /// Keep the returned enumeration single-threaded and complete enumeration promptly: the underlying
    /// DbContext must remain available for the duration of the enumeration.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="searchOrderDto"/> is null.</exception>
    public async IAsyncEnumerable<OrderDto> SearchOrdersStreamAsync(SearchOrderStreamDto searchOrderDto)
    {
        if (searchOrderDto is null) throw new ArgumentNullException(nameof(searchOrderDto));

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

        // Convert to DTO stream and yield results as they arrive from the database.
        IAsyncEnumerable<OrderDto> stream = query.Select(x => x.ToDto()).AsAsyncEnumerable();

        await foreach (OrderDto order in stream)
        {
            yield return order;
        }
    }

    /// <summary>
    /// Creates a new order after validation and persists it.
    /// </summary>
    /// <param name="orderDto">Order data to create. Must be valid.</param>
    /// <returns>The created <see cref="OrderDto"/> including the assigned identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderDto"/> is null.</exception>
    /// <exception cref="FluentValidation.ValidationException">Thrown when validation of <paramref name="orderDto"/> fails.</exception>
    public async Task<OrderDto> CreateOrderAsync(OrderDto orderDto)
    {
        if (orderDto is null) throw new ArgumentNullException(nameof(orderDto));

        var validation = await _orderValidator.ValidateAsync(orderDto);
        if (!validation.IsValid) throw new FluentValidation.ValidationException(validation.Errors);

        OrderEntity? orderEntity = orderDto.ToEntity();

        // assign a new identifier for the created order
        orderEntity.Id = Guid.NewGuid();

        await _context.Orders.AddAsync(orderEntity);
        await _context.SaveChangesAsync();

        return orderEntity.ToDto();
    }

    /// <summary>
    /// Updates an existing order identified by <paramref name="id"/> with the provided data.
    /// </summary>
    /// <param name="id">Identifier of the order to update.</param>
    /// <param name="orderDto">Updated order data. Must be valid.</param>
    /// <returns>The updated <see cref="OrderDto"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderDto"/> is null.</exception>
    /// <exception cref="FluentValidation.ValidationException">Thrown when validation of <paramref name="orderDto"/> fails.</exception>
    /// <exception cref="Exception">Thrown when the order with <paramref name="id"/> is not found.</exception>
    public async Task<OrderDto> UpdateOrderAsync(Guid id, OrderDto orderDto)
    {
        if (orderDto is null) throw new ArgumentNullException(nameof(orderDto));

        var validation = await _orderValidator.ValidateAsync(orderDto);
        if (!validation.IsValid) throw new FluentValidation.ValidationException(validation.Errors);

        OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

        if (orderEntity is null) throw new Exception("Order not found");

        OrderMapper.UpdateEntity(orderDto, orderEntity);

        await _context.SaveChangesAsync();

        return orderEntity.ToDto();
    }

    /// <summary>
    /// Deletes an order by identifier.
    /// </summary>
    /// <param name="id">Identifier of the order to delete.</param>
    /// <returns>A completed task when deletion is finished.</returns>
    /// <exception cref="Exception">Thrown when the order with <paramref name="id"/> is not found.</exception>
    public async Task DeleteOrderAsync(Guid id)
    {
        OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

        if (orderEntity is null) throw new Exception("Order not found");

        _context.Orders.Remove(orderEntity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Processes a stream of orders and creates or updates them in batches.
    /// </summary>
    /// <param name="batchOrders">An async stream of orders to be upserted. Must not be null.</param>
    /// <returns>A completed task when processing finishes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="batchOrders"/> is null.</exception>
    /// <exception cref="FluentValidation.ValidationException">Thrown when one or more items in the stream fail validation.</exception>
    public async Task CreateOrUpdateOrdersAsync(IAsyncEnumerable<OrderDto> batchOrders)
    {
        if (batchOrders is null) throw new ArgumentNullException(nameof(batchOrders));

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        var batch = new List<OrderDto>();
        const int batchSize = 1000;

        await foreach (OrderDto orderDto in batchOrders)
        {
            var result = await _orderValidator.ValidateAsync(orderDto);
            if (!result.IsValid)
            {
                // collect failures; do not stop processing immediately to report all validation issues
                failures.AddRange(result.Errors);
                continue;
            }

            batch.Add(orderDto);
            if (batch.Count >= batchSize)
            {
                if (failures.Any())
                    throw new FluentValidation.ValidationException(failures);

                await SaveBatchToDatabase(batch);
                batch.Clear();
            }
        }

        // Save remaining items; note that validation failures are raised only before final save.
        if (batch.Count > 0)
        {
            await SaveBatchToDatabase(batch);
        }
    }

    /// <summary>
    /// Seeds the database with test orders via the context implementation.
    /// </summary>
    /// <param name="seedNumber">Number of orders to seed.</param>
    public async Task SeedOrdersAsync(int seedNumber)
    {
        await _context.SeedOrdersAsync(seedNumber);
    }

    // SaveBatchToDatabase is an implementation detail: concise inline comments explain important behavior.
    private async Task SaveBatchToDatabase(List<OrderDto> batchOrders)
    {
        if (batchOrders is null) throw new ArgumentNullException(nameof(batchOrders));

        // For each DTO, attempt to find an existing entity by Id.
        // - If none exists, create a new entity and assign a new Id.
        // - If an entity exists, update it in-place via the mapper so EF Core tracks changes.
        foreach (OrderDto orderDto in batchOrders)
        {
            OrderEntity? orderEntity = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderDto.Id);

            if (orderEntity is null)
            {
                OrderEntity newOrderEntity = orderDto.ToEntity();
                newOrderEntity.Id = Guid.NewGuid();

                await _context.Orders.AddAsync(newOrderEntity);
            }
            else
            {
                // Update tracked entity properties from DTO
                OrderMapper.UpdateEntity(orderDto, orderEntity);
            }
        }

        // Persist the entire batch in a single SaveChanges call for efficiency.
        await _context.SaveChangesAsync();
    }
}