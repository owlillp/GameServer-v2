using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionService.Domain.Sessions;

namespace SessionService.Infrastructure.Postgres.Configurations;

internal sealed class SessionPlayerConfiguration : IEntityTypeConfiguration<SessionPlayer>
{
    public void Configure(EntityTypeBuilder<SessionPlayer> builder)
    {
        builder.ToTable("session_players");

        builder.Property(player => player.SessionId).HasColumnName("session_id");
        builder.Property(player => player.AccountId).HasColumnName("account_id");
        builder.Property(player => player.JoinedAt).HasColumnName("joined_at");
        builder.Property(player => player.LeftAt).HasColumnName("left_at");

        builder.HasKey(player => new { player.SessionId, player.AccountId });

        builder.HasIndex(player => player.AccountId)
            .HasFilter("left_at IS NULL")
            .HasDatabaseName("ix_session_players_present_account");

        builder.Ignore(player => player.IsPresent);
    }
}
