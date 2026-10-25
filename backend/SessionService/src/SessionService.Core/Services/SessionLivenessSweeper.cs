using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Domain.Sessions;
using Shared.Core.Database;

namespace SessionService.Core.Services;

/// <summary>
/// Closes sessions whose live key in Redis expired (the server stopped reporting them):
/// Active → Closed, remaining players are marked as left. Several replicas share the work
/// through <c>FOR UPDATE SKIP LOCKED</c>.
/// </summary>
public sealed class SessionLivenessSweeper(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<SessionOptions> options,
    TimeProvider timeProvider,
    ILogger<SessionLivenessSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(options.CurrentValue.LivenessSweepIntervalSeconds),
                    timeProvider,
                    stoppingToken);

                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Session liveness sweep failed");
            }
        }
    }

    internal async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var registry = scope.ServiceProvider.GetRequiredService<ISessionRegistry>();
        var publisher = scope.ServiceProvider.GetRequiredService<ISessionEventPublisher>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        SessionOptions current = options.CurrentValue;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var closed = new List<Session>();

        await SkipLockedBatch.RunAsync(
            transactionManager,
            token => sessionRepository.ClaimForLivenessCheckAsync(
                now.AddSeconds(-current.NewSessionTtlSeconds),
                current.LivenessBatchSize,
                token),
            async (sessions, token) =>
            {
                IReadOnlySet<Guid> alive = await registry.GetAliveAsync(
                    [.. sessions.Select(session => session.Id)],
                    token);

                foreach (Session session in sessions)
                {
                    if (alive.Contains(session.Id))
                    {
                        session.MarkAlive(now);
                        continue;
                    }

                    foreach (Guid accountId in session.Close(Constants.CloseReasons.EXPIRED, now))
                    {
                        await publisher.PublishPlayerLeftAsync(session, accountId, now);
                    }

                    await publisher.PublishSessionClosedAsync(session);
                    closed.Add(session);
                }

                (await transactionManager.SaveChangesAsync(token)).ThrowIfFailed();
            },
            cancellationToken);

        foreach (Guid sessionId in closed.Select(session => session.Id))
        {
            await registry.RemoveSessionAsync(sessionId, cancellationToken);
            logger.LogInformation("Session {SessionId} expired and was closed", sessionId);
        }
    }
}
