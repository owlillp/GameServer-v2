using EventService.Domain;
using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventRewardClaimConfiguration : IEntityTypeConfiguration<EventRewardClaim>
{
    public void Configure(EntityTypeBuilder<EventRewardClaim> builder)
    {
        builder.ToTable("event_reward_claims");

        builder.Property(claim => claim.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(claim => claim.EventId).HasColumnName("event_id");
        builder.Property(claim => claim.RewardId).HasColumnName("reward_id");

        builder.Property(claim => claim.RewardCode)
            .HasColumnName("reward_code")
            .HasMaxLength(EventConstants.CODE_MAX_LENGTH);

        builder.Property(claim => claim.AccountId).HasColumnName("account_id");
        builder.Property(claim => claim.PointsSpent).HasColumnName("points_spent");
        builder.Property(claim => claim.IsRepeatable).HasColumnName("is_repeatable");

        builder.Property(claim => claim.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(claim => claim.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(EventConstants.REASON_MAX_LENGTH);

        builder.Property(claim => claim.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(EventConstants.IDEMPOTENCY_KEY_MAX_LENGTH);

        builder.Property(claim => claim.CreatedAt).HasColumnName("created_at");
        builder.Property(claim => claim.CompletedAt).HasColumnName("completed_at");

        builder.HasKey(claim => claim.Id);

        builder.HasIndex(claim => new { claim.AccountId, claim.CreatedAt })
            .HasDatabaseName("ix_event_reward_claims_account_created_at");

        builder.HasIndex(claim => new { claim.AccountId, claim.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_event_reward_claims_account_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(claim => new { claim.AccountId, claim.RewardId })
            .IsUnique()
            .HasDatabaseName("uq_event_reward_claims_account_reward")
            .HasFilter("is_repeatable = false AND status <> 'FAILED'");
    }
}
