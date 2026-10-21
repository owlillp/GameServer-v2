using MatchmakingService.Domain;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchRewardDefinitionConfiguration : IEntityTypeConfiguration<MatchRewardDefinition>
{
    public void Configure(EntityTypeBuilder<MatchRewardDefinition> builder)
    {
        builder.ToTable("match_reward_definitions");

        builder.Property(definition => definition.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(definition => definition.MatchId).HasColumnName("match_id");
        builder.Property(definition => definition.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(MatchmakingConstants.CURRENCY_MAX_LENGTH);
        builder.Property(definition => definition.ParticipationAmount).HasColumnName("participation_amount");
        builder.Property(definition => definition.WinBonusAmount).HasColumnName("win_bonus_amount");
        builder.Property(definition => definition.CreatedAt).HasColumnName("created_at");

        builder.HasKey(definition => definition.Id);
        builder.HasIndex(definition => definition.MatchId)
            .IsUnique()
            .HasDatabaseName("uq_match_reward_definitions_match_id");

        builder.HasMany(definition => definition.Items)
            .WithOne()
            .HasForeignKey(item => item.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
