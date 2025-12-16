using Microsoft.AspNetCore.Mvc;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.DTOs;
using System.Text.Json;

namespace RELEX.InventoryManager.Api.Controllers;


[Route("api/[controller]")]
[ApiController]
public class OrdersController(ILogger<OrdersController> logger, IOrderManager orderManager) : Controller
{
    private readonly ILogger<OrdersController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOrderManager _orderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));

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

    [HttpGet]
    [ProducesResponseType(typeof(SearchOrderResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrdersAsync([FromQuery] SearchOrderDto searchRequest)
    {
        SearchOrderResultDto? searchOrderResultDto = await _orderManager.SearchOrdersAsync(searchRequest);

        return Ok(searchOrderResultDto);
    }


    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrderAsync([FromBody] OrderDto order)
    {
        OrderDto? createdOrder = await _orderManager.CreateOrderAsync(order);

        return Ok(createdOrder);
    }

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


    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteOrderAsync(Guid id)
    {
        await _orderManager.DeleteOrderAsync(id);

        return Ok();
    }


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
}
