using MatchmakingService.Domain.Leaderboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
    public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
    {
        builder.ToTable("leaderboard_entries");

        builder.Property(entry => entry.AccountId).HasColumnName("account_id").ValueGeneratedNever();
        builder.Property(entry => entry.Points).HasColumnName("points");
        builder.Property(entry => entry.Wins).HasColumnName("wins");
        builder.Property(entry => entry.MatchesPlayed).HasColumnName("matches_played");
        builder.Property(entry => entry.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(entry => entry.AccountId);
        builder.HasIndex(entry => entry.Points)
            .IsDescending(true)
            .HasDatabaseName("ix_leaderboard_entries_points");
    }
}
