using MatchmakingService.Domain;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchRewardGrantConfiguration : IEntityTypeConfiguration<MatchRewardGrant>
{
    public void Configure(EntityTypeBuilder<MatchRewardGrant> builder)
    {
        builder.ToTable("match_reward_grants");

        builder.Property(grant => grant.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(grant => grant.MatchId).HasColumnName("match_id");
        builder.Property(grant => grant.AccountId).HasColumnName("account_id");
        builder.Property(grant => grant.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(MatchmakingConstants.CURRENCY_MAX_LENGTH);
        builder.Property(grant => grant.Amount).HasColumnName("amount");
        builder.Property(grant => grant.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(grant => grant.WalletOperationId).HasColumnName("wallet_operation_id");
        builder.Property(grant => grant.WalletCreditedAt).HasColumnName("wallet_credited_at");
        builder.Property(grant => grant.ItemsPublishedAt).HasColumnName("items_published_at");
        builder.Property(grant => grant.ItemsGrantedAt).HasColumnName("items_granted_at");
        builder.Property(grant => grant.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(MatchmakingConstants.FAILURE_REASON_MAX_LENGTH);
        builder.Property(grant => grant.CreatedAt).HasColumnName("created_at");
        builder.Property(grant => grant.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(grant => grant.Id);

        // PostgreSQL row version: concurrent updates of the same row fail instead of overwriting each other.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();
        builder.HasIndex(grant => new { grant.MatchId, grant.AccountId })
            .IsUnique()
            .HasDatabaseName("uq_match_reward_grants_match_account");
        builder.HasIndex(grant => grant.Status).HasDatabaseName("ix_match_reward_grants_status");

        builder.HasMany(grant => grant.Items)
            .WithOne()
            .HasForeignKey(item => item.GrantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
