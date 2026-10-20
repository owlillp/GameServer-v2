using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventQuestProgressConfiguration : IEntityTypeConfiguration<EventQuestProgress>
{
    public void Configure(EntityTypeBuilder<EventQuestProgress> builder)
    {
        builder.ToTable("event_quest_progress");

        builder.Property(progress => progress.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(progress => progress.EventId).HasColumnName("event_id");
        builder.Property(progress => progress.QuestId).HasColumnName("quest_id");
        builder.Property(progress => progress.AccountId).HasColumnName("account_id");
        builder.Property(progress => progress.PeriodStart).HasColumnName("period_start");
        builder.Property(progress => progress.Progress).HasColumnName("progress");
        builder.Property(progress => progress.CompletedAt).HasColumnName("completed_at");
        builder.Property(progress => progress.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(progress => progress.Id);

        builder.HasIndex(progress => new { progress.QuestId, progress.AccountId, progress.PeriodStart })
            .IsUnique()
            .HasDatabaseName("uq_event_quest_progress_quest_account_period");

        builder.HasIndex(progress => new { progress.EventId, progress.AccountId })
            .HasDatabaseName("ix_event_quest_progress_event_account");
    }
}
