using MatchmakingService.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class PartyMemberConfiguration : IEntityTypeConfiguration<PartyMember>
{
    public void Configure(EntityTypeBuilder<PartyMember> builder)
    {
        builder.ToTable("party_members");

        builder.Property(member => member.PartyId).HasColumnName("party_id");
        builder.Property(member => member.AccountId).HasColumnName("account_id");
        builder.Property(member => member.IsReady).HasColumnName("is_ready");
        builder.Property(member => member.JoinedAt).HasColumnName("joined_at");

        builder.HasKey(member => new { member.PartyId, member.AccountId });
        builder.HasIndex(member => member.AccountId).HasDatabaseName("ix_party_members_account_id");
    }
}
