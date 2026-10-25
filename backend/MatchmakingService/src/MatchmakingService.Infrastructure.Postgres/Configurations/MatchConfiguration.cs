using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");

        builder.Property(match => match.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(match => match.Mode).HasColumnName("mode").HasMaxLength(MatchmakingConstants.MODE_MAX_LENGTH);
        builder.Property(match => match.Region).HasColumnName("region").HasMaxLength(MatchmakingConstants.REGION_MAX_LENGTH).HasDefaultValue(MatchmakingConstants.DEFAULT_REGION);
        builder.Property(match => match.AllocationRequestedAt).HasColumnName("allocation_requested_at");
        builder.Property(match => match.AllocatedAt).HasColumnName("allocated_at");
        builder.Property(match => match.SessionId).HasColumnName("session_id");
        builder.Property(match => match.ServerAddress).HasColumnName("server_address").HasMaxLength(MatchmakingConstants.ADDRESS_MAX_LENGTH);
        builder.Property(match => match.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(match => match.StartsAt).HasColumnName("starts_at");
        builder.Property(match => match.EndsAt).HasColumnName("ends_at");
        builder.Property(match => match.FinishedAt).HasColumnName("finished_at");
        builder.Property(match => match.WinnerPartyId).HasColumnName("winner_party_id");
        builder.Property(match => match.CreatedAt).HasColumnName("created_at");
        builder.Property(match => match.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(match => match.Id);

        // PostgreSQL row version: concurrent updates of the same row fail instead of overwriting each other.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasMany(match => match.Parties)
            .WithOne()
            .HasForeignKey(party => party.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(match => match.Participants)
            .WithOne()
            .HasForeignKey(participant => participant.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(match => match.Results)
            .WithOne()
            .HasForeignKey(result => result.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(match => match.Reward)
            .WithOne()
            .HasForeignKey<MatchRewardDefinition>(definition => definition.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(match => new { match.Status, match.EndsAt })
            .HasDatabaseName("ix_matches_status_ends_at");
        builder.HasIndex(match => match.WinnerPartyId).HasDatabaseName("ix_matches_winner_party_id");

        builder.HasIndex(match => new { match.Status, match.AllocationRequestedAt })
            .HasFilter("allocated_at IS NULL AND allocation_requested_at IS NOT NULL")
            .HasDatabaseName("ix_matches_waiting_for_server");

        builder.Ignore(match => match.HasResults);
        builder.Ignore(match => match.IsWaitingForServer);
        builder.Ignore(match => match.IsAllocated);
    }
}
