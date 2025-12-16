using RELEX.InventoryManager.BusinessManager.DTOs;

namespace RELEX.InventoryManager.BusinessManager.Contracts;

public interface IOrderManager
{
    public Task<OrderDto?> GetByIdAsync(Guid orderId);
    public Task<SearchOrderResultDto> SearchOrdersAsync(SearchOrderDto searchOrderDto);
    public Task<OrderDto> CreateOrderAsync(OrderDto orderDto);
    public Task<OrderDto> UpdateOrderAsync(Guid id, OrderDto orderDto);
    public Task DeleteOrderAsync(Guid id);
    public Task CreateOrUpdateOrdersAsync(IAsyncEnumerable<OrderDto> batchOrders);
}