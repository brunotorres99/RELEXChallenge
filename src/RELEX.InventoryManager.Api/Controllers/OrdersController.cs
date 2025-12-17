using Microsoft.AspNetCore.Mvc;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.DTOs;
using System.Text.Json;

namespace RELEX.InventoryManager.Api.Controllers;

/// <summary>
/// API controller that exposes CRUD and search operations for orders.
/// Uses an <see cref="IOrderManager"/> to perform business operations.
/// </summary>
/// <param name="logger">Logger for diagnostics and request-scoped logging.</param>
/// <param name="orderManager">Business manager that handles order operations.</param>
[Route("api/[controller]")]
[ApiController]
public class OrdersController(ILogger<OrdersController> logger, IOrderManager orderManager) : Controller
{
    private readonly ILogger<OrdersController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOrderManager _orderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));

    /// <summary>
    /// Retrieves a single order by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the order to retrieve.</param>
    /// <returns>
    /// 200 OK with the <see cref="OrderDto"/> when found; 404 NotFound when the order does not exist.
    /// </returns>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        OrderDto? order = await _orderManager.GetByIdAsync(id);

        if (order is null) return NotFound();

        return Ok(order);
    }

    /// <summary>
    /// Searches orders using query parameters supplied in <see cref="SearchOrderDto"/>.
    /// Supports paging and filtering as defined by the DTO.
    /// </summary>
    /// <param name="searchRequest">Search criteria bound from query string.</param>
    /// <returns>200 OK with a <see cref="SearchOrderResultDto"/> containing matching orders and metadata.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchOrderResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrdersAsync([FromQuery] SearchOrderDto searchRequest)
    {
        SearchOrderResultDto? searchOrderResultDto = await _orderManager.SearchOrdersAsync(searchRequest);

        return Ok(searchOrderResultDto);
    }

    /// <summary>
    /// Returns a stream/enumerable of orders that match the provided stream search criteria.
    /// This action returns the IAsyncEnumerable and relies on the manager to stream results efficiently.
    /// </summary>
    /// <param name="searchRequest">Stream search criteria bound from query string.</param>
    /// <returns>200 OK with an asynchronous stream of <see cref="OrderDto"/> items.</returns>
    [HttpGet]
    [Route("stream")]
    [ProducesResponseType(typeof(OrderDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrdersStreamAsync([FromQuery] SearchOrderStreamDto searchRequest)
    {
        return Ok(_orderManager.SearchOrdersStreamAsync(searchRequest));
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="order">Order data to create.</param>
    /// <returns>200 OK with the created <see cref="OrderDto"/>.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrderAsync([FromBody] OrderDto order)
    {
        OrderDto? createdOrder = await _orderManager.CreateOrderAsync(order);

        return Ok(createdOrder);
    }

    /// <summary>
    /// Updates an existing order identified by <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Identifier of the order to update.</param>
    /// <param name="order">Updated order data.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="OrderDto"/> when update succeeds;
    /// 400 BadRequest when the order is not found or update fails.
    /// </returns>
    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrderAsync(Guid id, [FromBody] OrderDto order)
    {
        OrderDto? updatedOrder = await _orderManager.UpdateOrderAsync(id, order);

        if (updatedOrder is null) return BadRequest("Order not found");

        return Ok(updatedOrder);
    }

    /// <summary>
    /// Deletes an order by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the order to delete.</param>
    /// <returns>200 OK when deletion completes (idempotent).</returns>
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteOrderAsync(Guid id)
    {
        await _orderManager.DeleteOrderAsync(id);

        return Ok();
    }

    /// <summary>
    /// Creates or updates orders in bulk by deserializing a JSON stream from the request body.
    /// Expects an array or JSON objects sequence representing <see cref="OrderDto"/>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="JsonSerializer.DeserializeAsyncEnumerable{T}"/> to support efficient streaming
    /// and low memory usage for large payloads. The request body is read as an async enumerable of <see cref="OrderDto"/>.
    /// </remarks>
    /// <returns>200 OK when processing completes successfully.</returns>
    [HttpPost]
    [Route("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrUpdateOrdersAsync()
    {
        IAsyncEnumerable<OrderDto?> batchOrders = JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(Request.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        await _orderManager.CreateOrUpdateOrdersAsync(batchOrders!);

        return Ok();
    }

    /// <summary>
    /// Seeds the datastore with a number of test orders.
    /// </summary>
    /// <param name="seedNumber">Number of orders to seed.</param>
    /// <returns>200 OK when seeding completes; 400 BadRequest for invalid inputs.</returns>
    [HttpPost]
    [Route("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteOrderAsync(int seedNumber)
    {
        await _orderManager.SeedOrdersAsync(seedNumber);

        return Ok();
    }
}
