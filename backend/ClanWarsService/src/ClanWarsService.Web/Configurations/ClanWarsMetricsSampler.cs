using Npgsql;
using Shared.Observability;

namespace ClanWarsService.Web.Configurations;

/// <summary>
/// Database gauges for dashboards and alerts, refreshed every 30 seconds.
/// </summary>
public sealed class ClanWarsMetricsSampler(
    IServiceScopeFactory scopeFactory,
    SampledGauges gauges,
    ILogger<ClanWarsMetricsSampler> logger) : GaugeSampler(scopeFactory, gauges, logger)
{
    protected override async Task SampleAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();

        await SampleGroupedAsync(dataSource, "clanwars_reward_grants", "Season reward grants by status", "status",
            "SELECT status, count(*) FROM clanwars.season_reward_grants GROUP BY status", cancellationToken);
        await SampleScalarAsync(dataSource, "clanwars_battle_queue_length", "Squads waiting for a battle",
            "SELECT count(*) FROM clanwars.battle_queue WHERE status = 'Queued'", cancellationToken);
        await SampleScalarAsync(dataSource, "clanwars_battles_open", "Battles scheduled or in progress",
            "SELECT count(*) FROM clanwars.battles WHERE status IN ('Scheduled', 'InProgress')", cancellationToken);
        await SampleScalarAsync(dataSource, "clanwars_seasons_rewarding", "Seasons waiting for reward delivery",
            "SELECT count(*) FROM clanwars.seasons WHERE status = 'Rewarding'", cancellationToken);
        await SampleDeadLettersAsync(dataSource, cancellationToken);
    }
}
