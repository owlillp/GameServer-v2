using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Postgres.Configurations;

internal sealed class EventProgressConfiguration : IEntityTypeConfiguration<EventProgress>
{
    public void Configure(EntityTypeBuilder<EventProgress> builder)
    {
        builder.ToTable("event_progress");

        builder.Property(progress => progress.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(progress => progress.EventId).HasColumnName("event_id");
        builder.Property(progress => progress.AccountId).HasColumnName("account_id");
        builder.Property(progress => progress.PointsBalance).HasColumnName("points_balance");
        builder.Property(progress => progress.TotalPointsEarned).HasColumnName("total_points_earned");
        builder.Property(progress => progress.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(progress => progress.CreatedAt).HasColumnName("created_at");
        builder.Property(progress => progress.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(progress => progress.Id);

        builder.HasIndex(progress => new { progress.EventId, progress.AccountId })
            .IsUnique()
            .HasDatabaseName("uq_event_progress_event_account");
    }
}
