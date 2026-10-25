using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionService.Domain;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;

namespace SessionService.Infrastructure.Postgres.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.ServerId).HasColumnName("server_id");
        builder.Property(session => session.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(16);
        builder.Property(session => session.Mode).HasColumnName("mode").HasMaxLength(SessionConstants.MODE_MAX_LENGTH);
        builder.Property(session => session.Region).HasColumnName("region").HasMaxLength(SessionConstants.REGION_MAX_LENGTH);
        builder.Property(session => session.MaxPlayers).HasColumnName("max_players");
        builder.Property(session => session.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(session => session.MatchId).HasColumnName("match_id");
        builder.Property(session => session.PlayerCount).HasColumnName("player_count");
        builder.Property(session => session.CreatedAt).HasColumnName("created_at");
        builder.Property(session => session.ClosedAt).HasColumnName("closed_at");
        builder.Property(session => session.CloseReason).HasColumnName("close_reason").HasMaxLength(SessionConstants.REASON_MAX_LENGTH);
        builder.Property(session => session.LivenessCheckedAt).HasColumnName("liveness_checked_at");

        builder.HasKey(session => session.Id);

        // Joins and leaves of one session are serialised by the row version: a concurrent
        // update fails instead of losing a player or the player count.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasOne<GameServer>()
            .WithMany()
            .HasForeignKey(session => session.ServerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(session => session.Players)
            .WithOne()
            .HasForeignKey(player => player.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(session => session.Players).AutoInclude();

        builder.HasIndex(session => new { session.Kind, session.Region, session.Mode, session.Status, session.PlayerCount })
            .HasDatabaseName("ix_sessions_joinable");
        builder.HasIndex(session => new { session.Status, session.LivenessCheckedAt })
            .HasDatabaseName("ix_sessions_status_liveness");
        builder.HasIndex(session => session.ServerId).HasDatabaseName("ix_sessions_server_id");
        builder.HasIndex(session => session.MatchId)
            .IsUnique()
            .HasFilter("match_id IS NOT NULL")
            .HasDatabaseName("uq_sessions_match_id");

        builder.Ignore(session => session.IsOpen);
        builder.Ignore(session => session.HasFreeSlot);
    }
}
