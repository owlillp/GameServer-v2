using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("parties");

        builder.Property(party => party.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(party => party.Code).HasColumnName("code").HasMaxLength(MatchmakingConstants.PARTY_CODE_LENGTH);
        builder.Property(party => party.LeaderAccountId).HasColumnName("leader_account_id");
        builder.Property(party => party.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(party => party.CreatedAt).HasColumnName("created_at");
        builder.Property(party => party.UpdatedAt).HasColumnName("updated_at");
        builder.Property(party => party.Version).HasColumnName("version");

        builder.HasKey(party => party.Id);

        // PostgreSQL row version: concurrent updates of the same row fail instead of overwriting each other.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();
        builder.HasIndex(party => party.Code).IsUnique().HasDatabaseName("uq_parties_code");

        builder.HasMany(party => party.Members)
            .WithOne()
            .HasForeignKey(member => member.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(party => party.AllReady);
        builder.Ignore(party => party.CanQueue);
        builder.Ignore(party => party.MemberAccountIds);
    }
}
