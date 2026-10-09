using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class OutfitEntryConfiguration : IEntityTypeConfiguration<OutfitEntry>
{
    public void Configure(EntityTypeBuilder<OutfitEntry> builder)
    {
        builder.ToTable("outfit_entries");

        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.AccountId).HasColumnName("account_id");

        builder.Property(entry => entry.Slot)
            .HasColumnName("slot")
            .HasMaxLength(InventoryConstants.SLOT_MAX_LENGTH)
            .IsRequired();

        builder.Property(entry => entry.ItemId).HasColumnName("item_id");

        builder.HasKey(entry => entry.Id);

        builder.HasIndex(entry => new { entry.AccountId, entry.Slot })
            .IsUnique()
            .HasDatabaseName("uq_outfit_entries_account_slot");
    }
}
