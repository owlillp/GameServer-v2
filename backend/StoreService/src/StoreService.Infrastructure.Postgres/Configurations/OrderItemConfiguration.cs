using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreService.Domain;
using StoreService.Domain.Orders;

namespace StoreService.Infrastructure.Postgres.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.OrderId).HasColumnName("order_id");
        builder.Property(item => item.CatalogItemId).HasColumnName("catalog_item_id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(StoreConstants.CODE_MAX_LENGTH);

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasMaxLength(32);

        builder.Property(item => item.Slot)
            .HasColumnName("slot")
            .HasMaxLength(32);

        builder.Property(item => item.Quantity).HasColumnName("quantity");

        builder.HasKey(item => item.Id);
    }
}
