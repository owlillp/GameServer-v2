using MatchmakingService.Domain;
using MatchmakingService.Domain.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class QueueTicketConfiguration : IEntityTypeConfiguration<QueueTicket>
{
    public void Configure(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("queue_tickets");

        builder.Property(ticket => ticket.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ticket => ticket.PartyId).HasColumnName("party_id");
        builder.Property(ticket => ticket.LeaderAccountId).HasColumnName("leader_account_id");
        builder.Property(ticket => ticket.PartySize).HasColumnName("party_size");
        builder.Property(ticket => ticket.Mode).HasColumnName("mode").HasMaxLength(MatchmakingConstants.MODE_MAX_LENGTH);
        builder.Property(ticket => ticket.Region).HasColumnName("region").HasMaxLength(MatchmakingConstants.REGION_MAX_LENGTH).HasDefaultValue(MatchmakingConstants.DEFAULT_REGION);
        builder.Property(ticket => ticket.IsPriority).HasColumnName("is_priority");
        builder.Property(ticket => ticket.Attempt).HasColumnName("attempt");
        builder.Property(ticket => ticket.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(ticket => ticket.MatchId).HasColumnName("match_id");
        builder.Property(ticket => ticket.EnqueuedAt).HasColumnName("enqueued_at");
        builder.Property(ticket => ticket.MatchedAt).HasColumnName("matched_at");

        builder.HasKey(ticket => ticket.Id);

        // PostgreSQL row version: concurrent updates of the same row fail instead of overwriting each other.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();
        builder.HasIndex(ticket => ticket.MatchId).HasDatabaseName("ix_queue_tickets_match_id");
        builder.HasIndex(ticket => ticket.PartyId).HasDatabaseName("ix_queue_tickets_party_id");
        builder.HasIndex(ticket => new { ticket.Status, ticket.EnqueuedAt })
            .HasDatabaseName("ix_queue_tickets_status_enqueued_at");
    }
}
