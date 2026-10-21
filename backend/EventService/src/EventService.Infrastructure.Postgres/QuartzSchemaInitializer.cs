using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EventService.Infrastructure.Postgres;

public static class QuartzSchemaInitializer
{
    private const string SCHEMA_RESOURCE = "EventService.Infrastructure.Postgres.Quartz.quartz_schema.sql";

    public static async Task EnsureQuartzSchemaAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using (var checkCommand = new NpgsqlCommand(
            "SELECT to_regclass('public.qrtz_job_details') IS NOT NULL",
            connection))
        {
            bool exists = (bool)(await checkCommand.ExecuteScalarAsync(cancellationToken) ?? false);
            if (exists)
            {
                return;
            }
        }

        await using Stream stream = typeof(QuartzSchemaInitializer).Assembly
            .GetManifestResourceStream(SCHEMA_RESOURCE)
            ?? throw new InvalidOperationException($"Embedded resource '{SCHEMA_RESOURCE}' was not found.");

        using var reader = new StreamReader(stream);
        string sql = await reader.ReadToEndAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
