using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionService.Domain;
using SessionService.Domain.Servers;

namespace SessionService.Infrastructure.Postgres.Configurations;

internal sealed class GameServerConfiguration : IEntityTypeConfiguration<GameServer>
{
    public void Configure(EntityTypeBuilder<GameServer> builder)
    {
        builder.ToTable("game_servers");

        builder.Property(server => server.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(server => server.Address).HasColumnName("address").HasMaxLength(SessionConstants.ADDRESS_MAX_LENGTH);
        builder.Property(server => server.Region).HasColumnName("region").HasMaxLength(SessionConstants.REGION_MAX_LENGTH);
        builder.Property(server => server.Modes).HasColumnName("modes").HasColumnType("text[]");
        builder.Property(server => server.Capacity).HasColumnName("capacity");
        builder.Property(server => server.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(server => server.RegisteredAt).HasColumnName("registered_at");
        builder.Property(server => server.LastHeartbeatAt).HasColumnName("last_heartbeat_at");

        builder.HasKey(server => server.Id);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasIndex(server => new { server.Region, server.Status, server.LastHeartbeatAt })
            .HasDatabaseName("ix_game_servers_region_status_heartbeat");
    }
}
