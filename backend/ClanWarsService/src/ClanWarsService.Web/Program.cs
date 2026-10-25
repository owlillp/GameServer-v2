using Serilog;
using Shared.Framework.Logging;
using ClanWarsService.Infrastructure.Postgres;
using ClanWarsService.Web.Configurations;
using Constants = ClanWarsService.Web.Configurations.Constants;

Log.Logger = LoggingExtensions.CreateBootstrapLogger(Constants.SERVICE_NAME);

try
{
    Log.Information("Starting application: {ServiceName}", Constants.SERVICE_NAME);

    var builder = WebApplication.CreateBuilder(args);

    builder.AddDependency(builder.Configuration);

    var app = builder.Build();

    await app.Services.EnsureQuartzSchemaAsync();

    app.Configure();

    await app.RunAsync();
}
catch (HostAbortedException)
{
    // Expected when running EF migrations tooling
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

namespace ClanWarsService.Web
{
    public class Program;
}
