using EventService.Domain;
using EventService.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventQuestConfiguration : IEntityTypeConfiguration<EventQuest>
{
    public void Configure(EntityTypeBuilder<EventQuest> builder)
    {
        builder.ToTable("event_quests");

        builder.Property(quest => quest.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(quest => quest.EventId).HasColumnName("event_id");

        builder.Property(quest => quest.Code)
            .HasColumnName("code")
            .HasMaxLength(EventConstants.CODE_MAX_LENGTH);

        builder.Property(quest => quest.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

        builder.Property(quest => quest.Description)
            .HasColumnName("description")
            .HasMaxLength(EventConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(quest => quest.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(quest => quest.Metric)
            .HasColumnName("metric")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(quest => quest.Target).HasColumnName("target");
        builder.Property(quest => quest.Points).HasColumnName("points");
        builder.Property(quest => quest.SortOrder).HasColumnName("sort_order");

        builder.HasKey(quest => quest.Id);

        builder.HasIndex(quest => new { quest.EventId, quest.Code })
            .IsUnique()
            .HasDatabaseName("uq_event_quests_event_code");
    }
}
