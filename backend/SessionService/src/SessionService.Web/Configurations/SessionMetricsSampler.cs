using Npgsql;
using Shared.Observability;

namespace SessionService.Web.Configurations;

/// <summary>
/// Database gauges for dashboards and alerts, refreshed every 30 seconds.
/// </summary>
public sealed class SessionMetricsSampler(
    IServiceScopeFactory scopeFactory,
    SampledGauges gauges,
    ILogger<SessionMetricsSampler> logger) : GaugeSampler(scopeFactory, gauges, logger)
{
    protected override async Task SampleAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();

        await SampleGroupedAsync(dataSource, "session_sessions_open", "Open sessions by kind", "kind",
            "SELECT kind, count(*) FROM sessions.sessions WHERE status IN ('Allocating', 'Active') GROUP BY kind", cancellationToken);
        await SampleScalarAsync(dataSource, "session_players_online", "Players inside open sessions",
            "SELECT COALESCE(sum(player_count), 0) FROM sessions.sessions WHERE status IN ('Allocating', 'Active')", cancellationToken);
        await SampleScalarAsync(dataSource, "session_game_servers_online", "Game servers with a recent heartbeat",
            "SELECT count(*) FROM sessions.game_servers WHERE status = 'Online' AND last_heartbeat_at > now() - interval '2 minutes'", cancellationToken);
        await SampleScalarAsync(dataSource, "session_reports_suspicious_24h", "Suspicious match reports in the last 24 hours",
            "SELECT count(*) FROM sessions.raw_reports WHERE is_suspicious AND received_at > now() - interval '24 hours'", cancellationToken);
        await SampleDeadLettersAsync(dataSource, cancellationToken);
    }
}
