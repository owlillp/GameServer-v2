using MatchmakingService.Domain;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class MatchRewardGrantItemConfiguration : IEntityTypeConfiguration<MatchRewardGrantItem>
{
    public void Configure(EntityTypeBuilder<MatchRewardGrantItem> builder)
    {
        builder.ToTable("match_reward_grant_items");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.GrantId).HasColumnName("grant_id");
        builder.Property(item => item.ItemId).HasColumnName("item_id");
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(MatchmakingConstants.ITEM_CODE_MAX_LENGTH);
        builder.Property(item => item.Kind).HasColumnName("kind").HasMaxLength(MatchmakingConstants.ITEM_KIND_MAX_LENGTH);
        builder.Property(item => item.Slot).HasColumnName("slot").HasMaxLength(MatchmakingConstants.SLOT_MAX_LENGTH);
        builder.Property(item => item.Quantity).HasColumnName("quantity");

        builder.HasKey(item => item.Id);
    }
}
