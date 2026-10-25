using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Shared.Observability;

/// <summary>
/// Refreshes sampled gauges every <see cref="Interval"/>. A failed sample is logged and the
/// previous values stay, so a database hiccup does not reset the dashboards to zero.
/// </summary>
public abstract class GaugeSampler(
    IServiceScopeFactory scopeFactory,
    SampledGauges gauges,
    ILogger logger) : BackgroundService
{
    public const string DEAD_LETTERS_GAUGE = "wolverine_dead_letters";

    protected virtual TimeSpan Interval => TimeSpan.FromSeconds(30);

    protected SampledGauges Gauges => gauges;

    protected abstract Task SampleAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await SampleAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Metrics sample failed");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    /// <summary>
    /// Messages Wolverine gave up on (its dead letter table), for the DLQ alert.
    /// </summary>
    protected async Task SampleDeadLettersAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(
            "SELECT CASE WHEN to_regclass('public.wolverine_dead_letters') IS NULL THEN 0 ELSE (SELECT count(*) FROM public.wolverine_dead_letters) END");

        long count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
        gauges.Set(DEAD_LETTERS_GAUGE, "Messages moved to the Wolverine dead letter queue", count);
    }

    /// <summary>
    /// <c>SELECT key, count</c> rows → one measurement per key under <paramref name="tag"/>.
    /// </summary>
    protected async Task SampleGroupedAsync(
        NpgsqlDataSource dataSource,
        string gauge,
        string description,
        string tag,
        string sql,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        var measurements = new List<System.Diagnostics.Metrics.Measurement<long>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            measurements.Add(new(reader.GetInt64(1), new KeyValuePair<string, object?>(tag, reader.GetString(0))));
        }

        gauges.Set(gauge, description, measurements);
    }

    protected async Task SampleScalarAsync(NpgsqlDataSource dataSource, string gauge, string description, string sql, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        long value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
        gauges.Set(gauge, description, value);
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
