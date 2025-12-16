using RELEX.InventoryManager.BusinessManager.DTOs;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.BusinessManager.Mappers;

public static class OrderMapper
{
    public static OrderEntity ToEntity(this OrderDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        return new OrderEntity
        {
            Id = dto.Id,
            LocationCode = dto.LocationCode,
            ProductCode = dto.ProductCode,
            OrderDate = dto.OrderDate,
            Quantity = dto.Quantity,
            SubmittedBy = dto.SubmittedBy,
            SubmittedAt = dto.SubmittedAt
        };
    }

    public static OrderDto ToDto(this OrderEntity entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        return new OrderDto
        {
            Id = entity.Id,
            LocationCode = entity.LocationCode,
            ProductCode = entity.ProductCode,
            OrderDate = entity.OrderDate,
            Quantity = entity.Quantity,
            SubmittedBy = entity.SubmittedBy,
            SubmittedAt = entity.SubmittedAt
        };
    }

    public static void UpdateEntity(this OrderDto dto, OrderEntity entity)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        // Keep Id unchanged when updating an existing entity.
        entity.LocationCode = dto.LocationCode;
        entity.ProductCode = dto.ProductCode;
        entity.OrderDate = dto.OrderDate;
        entity.Quantity = dto.Quantity;
        entity.SubmittedBy = dto.SubmittedBy;
        entity.SubmittedAt = dto.SubmittedAt;
    }
}