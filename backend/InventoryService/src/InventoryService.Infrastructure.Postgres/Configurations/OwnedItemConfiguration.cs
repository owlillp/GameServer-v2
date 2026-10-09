using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class OwnedItemConfiguration : IEntityTypeConfiguration<OwnedItem>
{
    public void Configure(EntityTypeBuilder<OwnedItem> builder)
    {
        builder.ToTable("owned_items");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccountId).HasColumnName("account_id");
        builder.Property(item => item.ItemId).HasColumnName("item_id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(InventoryConstants.CODE_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasMaxLength(InventoryConstants.KIND_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Slot)
            .HasColumnName("slot")
            .HasMaxLength(InventoryConstants.SLOT_MAX_LENGTH);

        builder.Property(item => item.Source)
            .HasColumnName("source")
            .HasMaxLength(InventoryConstants.SOURCE_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Quantity).HasColumnName("quantity");
        builder.Property(item => item.AcquiredAt).HasColumnName("acquired_at");

        builder.HasKey(item => item.Id);

        builder.HasIndex(item => new { item.AccountId, item.ItemId })
            .IsUnique()
            .HasDatabaseName("uq_owned_items_account_item");
    }
}
