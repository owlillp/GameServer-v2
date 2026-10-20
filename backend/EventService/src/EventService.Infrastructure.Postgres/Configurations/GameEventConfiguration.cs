using EventService.Domain;
using EventService.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class GameEventConfiguration : IEntityTypeConfiguration<GameEvent>
{
    public void Configure(EntityTypeBuilder<GameEvent> builder)
    {
        builder.ToTable("game_events");

        builder.Property(gameEvent => gameEvent.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(gameEvent => gameEvent.Code)
            .HasColumnName("code")
            .HasMaxLength(EventConstants.CODE_MAX_LENGTH);

        builder.Property(gameEvent => gameEvent.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

        builder.Property(gameEvent => gameEvent.Description)
            .HasColumnName("description")
            .HasMaxLength(EventConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(gameEvent => gameEvent.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(gameEvent => gameEvent.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(gameEvent => gameEvent.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(EventConstants.CURRENCY_MAX_LENGTH);

        builder.Property(gameEvent => gameEvent.AnnounceAt).HasColumnName("announce_at");
        builder.Property(gameEvent => gameEvent.StartsAt).HasColumnName("starts_at");
        builder.Property(gameEvent => gameEvent.EndsAt).HasColumnName("ends_at");
        builder.Property(gameEvent => gameEvent.GraceEndsAt).HasColumnName("grace_ends_at");
        builder.Property(gameEvent => gameEvent.StartedNotifiedAt).HasColumnName("started_notified_at");
        builder.Property(gameEvent => gameEvent.EndedNotifiedAt).HasColumnName("ended_notified_at");
        builder.Property(gameEvent => gameEvent.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(gameEvent => gameEvent.CreatedAt).HasColumnName("created_at");
        builder.Property(gameEvent => gameEvent.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(gameEvent => gameEvent.Id);

        builder.HasIndex(gameEvent => gameEvent.Code)
            .IsUnique()
            .HasDatabaseName("uq_game_events_code");

        builder.HasIndex(gameEvent => new { gameEvent.Status, gameEvent.StartsAt, gameEvent.GraceEndsAt })
            .HasDatabaseName("ix_game_events_status_schedule");

        builder.HasMany(gameEvent => gameEvent.Quests)
            .WithOne()
            .HasForeignKey(quest => quest.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(gameEvent => gameEvent.Quests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(gameEvent => gameEvent.Rewards)
            .WithOne()
            .HasForeignKey(reward => reward.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(gameEvent => gameEvent.Rewards)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
