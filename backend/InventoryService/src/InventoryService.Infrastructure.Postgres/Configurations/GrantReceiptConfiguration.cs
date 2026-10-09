using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class GrantReceiptConfiguration : IEntityTypeConfiguration<GrantReceipt>
{
    public void Configure(EntityTypeBuilder<GrantReceipt> builder)
    {
        builder.ToTable("grant_receipts");

        builder.Property(receipt => receipt.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(receipt => receipt.AccountId).HasColumnName("account_id");
        builder.Property(receipt => receipt.OperationId).HasColumnName("operation_id");

        builder.Property(receipt => receipt.Source)
            .HasColumnName("source")
            .HasMaxLength(InventoryConstants.SOURCE_MAX_LENGTH)
            .IsRequired();

        builder.Property(receipt => receipt.ItemCount).HasColumnName("item_count");
        builder.Property(receipt => receipt.CreatedAt).HasColumnName("created_at");

        builder.HasKey(receipt => receipt.Id);

        builder.HasIndex(receipt => receipt.OperationId)
            .IsUnique()
            .HasDatabaseName("uq_grant_receipts_operation");
    }
}
