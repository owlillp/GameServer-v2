using EventService.Domain;
using EventService.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventRewardConfiguration : IEntityTypeConfiguration<EventReward>
{
    public void Configure(EntityTypeBuilder<EventReward> builder)
    {
        builder.ToTable("event_rewards");

        builder.Property(reward => reward.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(reward => reward.EventId).HasColumnName("event_id");

        builder.Property(reward => reward.Code)
            .HasColumnName("code")
            .HasMaxLength(EventConstants.CODE_MAX_LENGTH);

        builder.Property(reward => reward.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

        builder.Property(reward => reward.Description)
            .HasColumnName("description")
            .HasMaxLength(EventConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(reward => reward.PointsCost).HasColumnName("points_cost");
        builder.Property(reward => reward.CatalogItemId).HasColumnName("catalog_item_id");

        builder.Property(reward => reward.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(EventConstants.ITEM_CODE_MAX_LENGTH);

        builder.Property(reward => reward.ItemKind)
            .HasColumnName("item_kind")
            .HasMaxLength(EventConstants.ITEM_KIND_MAX_LENGTH);

        builder.Property(reward => reward.Slot)
            .HasColumnName("slot")
            .HasMaxLength(EventConstants.SLOT_MAX_LENGTH);

        builder.Property(reward => reward.Quantity).HasColumnName("quantity");
        builder.Property(reward => reward.SortOrder).HasColumnName("sort_order");
        builder.Property(reward => reward.IsRepeatable).HasColumnName("is_repeatable");

        builder.HasKey(reward => reward.Id);

        builder.HasIndex(reward => new { reward.EventId, reward.Code })
            .IsUnique()
            .HasDatabaseName("uq_event_rewards_event_code");
    }
}
