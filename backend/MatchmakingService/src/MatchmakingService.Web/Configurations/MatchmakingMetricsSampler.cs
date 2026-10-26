using Npgsql;
using Shared.Observability;

namespace MatchmakingService.Web.Configurations;

/// <summary>
/// Database gauges for dashboards and alerts, refreshed every 30 seconds.
/// </summary>
public sealed class MatchmakingMetricsSampler(
    IServiceScopeFactory scopeFactory,
    SampledGauges gauges,
    ILogger<MatchmakingMetricsSampler> logger) : GaugeSampler(scopeFactory, gauges, logger)
{
    protected override async Task SampleAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();

        await SampleGroupedAsync(dataSource, "matchmaking_queue_length", "Parties waiting in the queue by mode", "mode",
            "SELECT mode, count(*) FROM matchmaking.queue_tickets WHERE status = 'Queued' GROUP BY mode", cancellationToken);
        await SampleScalarAsync(dataSource, "matchmaking_matches_waiting_for_server", "Matches waiting for a game server",
            "SELECT count(*) FROM matchmaking.matches WHERE status = 'Active' AND allocation_requested_at IS NOT NULL AND allocated_at IS NULL", cancellationToken);
        await SampleScalarAsync(dataSource, "matchmaking_matches_active", "Active matches",
            "SELECT count(*) FROM matchmaking.matches WHERE status = 'Active'", cancellationToken);
        await SampleGroupedAsync(dataSource, "matchmaking_reward_grants", "Match reward grants by status", "status",
            "SELECT status, count(*) FROM matchmaking.match_reward_grants GROUP BY status", cancellationToken);
        await SampleDeadLettersAsync(dataSource, cancellationToken);
    }
}
