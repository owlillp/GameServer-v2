using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionService.Domain;
using SessionService.Domain.Reports;
using SessionService.Domain.Sessions;

namespace SessionService.Infrastructure.Postgres.Configurations;

internal sealed class RawReportConfiguration : IEntityTypeConfiguration<RawReport>
{
    public void Configure(EntityTypeBuilder<RawReport> builder)
    {
        builder.ToTable("raw_reports");

        builder.Property(report => report.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(report => report.SessionId).HasColumnName("session_id");
        builder.Property(report => report.ServerId).HasColumnName("server_id");
        builder.Property(report => report.ActivityId).HasColumnName("activity_id").HasMaxLength(SessionConstants.ACTIVITY_ID_MAX_LENGTH);
        builder.Property(report => report.MatchId).HasColumnName("match_id");
        builder.Property(report => report.Mode).HasColumnName("mode").HasMaxLength(SessionConstants.MODE_MAX_LENGTH);
        builder.Property(report => report.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(report => report.Flags).HasColumnName("flags").HasColumnType("text[]");
        builder.Property(report => report.IsSuspicious).HasColumnName("is_suspicious");
        builder.Property(report => report.ReceivedAt).HasColumnName("received_at");

        builder.HasKey(report => report.Id);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(report => report.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(report => report.ActivityId)
            .IsUnique()
            .HasDatabaseName("uq_raw_reports_activity_id");
        builder.HasIndex(report => report.SessionId).HasDatabaseName("ix_raw_reports_session_id");
        builder.HasIndex(report => report.ReceivedAt)
            .HasFilter("is_suspicious")
            .HasDatabaseName("ix_raw_reports_suspicious");
    }
}
