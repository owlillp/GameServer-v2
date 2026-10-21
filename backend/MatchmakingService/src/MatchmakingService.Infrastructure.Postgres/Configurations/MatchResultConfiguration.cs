using MatchmakingService.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchResultConfiguration : IEntityTypeConfiguration<MatchResult>
{
    public void Configure(EntityTypeBuilder<MatchResult> builder)
    {
        builder.ToTable("match_results");

        builder.Property(result => result.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(result => result.MatchId).HasColumnName("match_id");
        builder.Property(result => result.AccountId).HasColumnName("account_id");
        builder.Property(result => result.PartyId).HasColumnName("party_id");
        builder.Property(result => result.Score).HasColumnName("score");
        builder.Property(result => result.Kills).HasColumnName("kills");
        builder.Property(result => result.IsWin).HasColumnName("is_win");
        builder.Property(result => result.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(result => result.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(result => result.Id);
        builder.HasIndex(result => new { result.MatchId, result.AccountId })
            .IsUnique()
            .HasDatabaseName("uq_match_results_match_account");
    }
}
