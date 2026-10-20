using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventMatchReceiptConfiguration : IEntityTypeConfiguration<EventMatchReceipt>
{
    public void Configure(EntityTypeBuilder<EventMatchReceipt> builder)
    {
        builder.ToTable("event_match_receipts");

        builder.Property(receipt => receipt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(receipt => receipt.EventId).HasColumnName("event_id");
        builder.Property(receipt => receipt.AccountId).HasColumnName("account_id");
        builder.Property(receipt => receipt.MatchId).HasColumnName("match_id");
        builder.Property(receipt => receipt.ProcessedAt).HasColumnName("processed_at");

        builder.HasKey(receipt => receipt.Id);

        builder.HasIndex(receipt => new { receipt.EventId, receipt.AccountId, receipt.MatchId })
            .IsUnique()
            .HasDatabaseName("uq_event_match_receipts_event_account_match");
    }
}
