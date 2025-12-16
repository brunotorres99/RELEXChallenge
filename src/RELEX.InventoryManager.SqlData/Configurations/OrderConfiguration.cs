using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RELEX.InventoryManager.SqlData.Entities;

namespace RELEX.InventoryManager.SqlData.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("Orders", "inv");

        builder.HasKey(p => p.Id);

        builder
            .Property(p => p.LocationCode)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder
            .Property(p => p.ProductCode)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder
            .Property(p => p.OrderDate)
            .HasColumnType("date")
            .IsRequired();

        builder
            .Property(p => p.Quantity)
            .HasColumnType("integer")
            .IsRequired();

        builder
            .Property(p => p.SubmittedBy)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder
            .Property(p => p.SubmittedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(x => x.LocationCode, "IX_Order_LocationCode");

        builder
            .HasIndex(x => x.ProductCode, "IX_Order_ProductCode");

        builder
            .HasIndex(x => x.OrderDate, "IX_Order_OrderDate");
    }
}
