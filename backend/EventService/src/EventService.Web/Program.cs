using Serilog;
using Shared.Framework.Logging;
using EventService.Infrastructure.Postgres;
using EventService.Web.Configurations;
using Constants = EventService.Web.Configurations.Constants;

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

namespace EventService.Web
{
    public class Program;
}
