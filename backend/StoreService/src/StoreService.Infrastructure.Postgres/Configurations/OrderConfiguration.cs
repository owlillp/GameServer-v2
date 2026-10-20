using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreService.Domain;
using StoreService.Domain.Orders;

namespace StoreService.Infrastructure.Postgres.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.Property(order => order.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(order => order.AccountId).HasColumnName("account_id");
        builder.Property(order => order.OfferId).HasColumnName("offer_id");

        builder.Property(order => order.OfferCode)
            .HasColumnName("offer_code")
            .HasMaxLength(StoreConstants.CODE_MAX_LENGTH);

        builder.Property(order => order.OfferName)
            .HasColumnName("offer_name")
            .HasMaxLength(StoreConstants.DISPLAY_NAME_MAX_LENGTH);

        builder.Property(order => order.Currency)
            .HasColumnName("currency")
            .HasMaxLength(StoreConstants.CURRENCY_MAX_LENGTH);

        builder.Property(order => order.TotalAmount).HasColumnName("total_amount");

        builder.Property(order => order.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(24);

        builder.Property(order => order.HoldId).HasColumnName("hold_id");

        builder.Property(order => order.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(StoreConstants.REASON_MAX_LENGTH);

        builder.Property(order => order.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(StoreConstants.IDEMPOTENCY_KEY_MAX_LENGTH);

        builder.Property(order => order.CreatedAt).HasColumnName("created_at");
        builder.Property(order => order.UpdatedAt).HasColumnName("updated_at");
        builder.Property(order => order.CompletedAt).HasColumnName("completed_at");

        builder.HasKey(order => order.Id);

        builder.HasIndex(order => new { order.AccountId, order.CreatedAt })
            .HasDatabaseName("ix_orders_account_created_at");

        builder.HasIndex(order => new { order.AccountId, order.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("uq_orders_account_idempotency_key");

        builder.HasIndex(order => new { order.Status, order.CreatedAt })
            .HasDatabaseName("ix_orders_status_created_at");

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
