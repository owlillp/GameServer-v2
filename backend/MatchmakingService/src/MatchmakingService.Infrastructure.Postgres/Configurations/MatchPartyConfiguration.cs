using MatchmakingService.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchPartyConfiguration : IEntityTypeConfiguration<MatchParty>
{
    public void Configure(EntityTypeBuilder<MatchParty> builder)
    {
        builder.ToTable("match_parties");

        builder.Property(party => party.MatchId).HasColumnName("match_id");
        builder.Property(party => party.PartyId).HasColumnName("party_id");
        builder.Property(party => party.Side).HasColumnName("side");

        builder.HasKey(party => new { party.MatchId, party.PartyId });
        builder.HasIndex(party => party.PartyId).HasDatabaseName("ix_match_parties_party_id");
    }
}
