using MatchmakingService.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("match_participants");

        builder.Property(participant => participant.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(participant => participant.MatchId).HasColumnName("match_id");
        builder.Property(participant => participant.PartyId).HasColumnName("party_id");
        builder.Property(participant => participant.AccountId).HasColumnName("account_id");

        builder.HasKey(participant => participant.Id);
        builder.HasIndex(participant => new { participant.MatchId, participant.AccountId })
            .IsUnique()
            .HasDatabaseName("uq_match_participants_match_account");
        builder.HasIndex(participant => participant.AccountId)
            .HasDatabaseName("ix_match_participants_account_id");
    }
}
