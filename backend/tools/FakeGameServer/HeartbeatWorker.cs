using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Grpc.Session;

namespace FakeGameServer;

/// <summary>
/// Registers the server and keeps a heartbeat stream open; on any failure it registers again.
/// </summary>
public sealed class HeartbeatWorker(
    SessionClient sessionClient,
    ServerState state,
    IOptions<FakeGameServerOptions> options,
    ILogger<HeartbeatWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int interval = await RegisterAsync(stoppingToken);
                await StreamHeartbeatsAsync(TimeSpan.FromSeconds(interval), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Session connection failed, retrying in {Delay}", RETRY_DELAY);
                await Task.Delay(RETRY_DELAY, stoppingToken);
            }
        }
    }

    private async Task<int> RegisterAsync(CancellationToken cancellationToken)
    {
        FakeGameServerOptions current = options.Value;

        var request = new RegisterRequest
        {
            ServerId = string.IsNullOrWhiteSpace(state.ServerId) ? current.ServerId : state.ServerId,
            Address = current.PublicAddress,
            Region = current.Region,
            Capacity = current.Capacity,
        };
        request.Modes.AddRange(current.Modes);

        RegisterReply reply = await sessionClient.UnaryAsync(
            callOptions => sessionClient.Raw.RegisterAsync(request, callOptions),
            cancellationToken);

        state.ServerId = reply.ServerId;
        logger.LogInformation(
            "Registered as {ServerId}, heartbeat every {Interval}s",
            reply.ServerId,
            reply.HeartbeatIntervalSeconds);

        return Math.Max(1, reply.HeartbeatIntervalSeconds);
    }

    private async Task StreamHeartbeatsAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        TimeSpan emptyTimeout = TimeSpan.FromSeconds(options.Value.EmptySessionSeconds);
        CallOptions callOptions = await sessionClient.CallOptionsAsync(cancellationToken);

        using AsyncClientStreamingCall<HeartbeatRequest, HeartbeatReply> call = sessionClient.Raw.Heartbeat(callOptions);
        using var timer = new PeriodicTimer(interval);

        // A token expires long before the stream would; reopen the stream every few minutes.
        DateTime reopenAt = DateTime.UtcNow.AddMinutes(5);

        do
        {
            DateTime now = DateTime.UtcNow;
            state.DropIdle(now, emptyTimeout);

            var heartbeat = new HeartbeatRequest
            {
                ServerId = state.ServerId,
                PlayerCount = state.Sessions.Values.Sum(session => session.Players.Count),
            };
            heartbeat.SessionIds.AddRange(state.LiveSessionIds(now, emptyTimeout).Select(id => id.ToString()));

            await call.RequestStream.WriteAsync(heartbeat, cancellationToken);
        }
        while (DateTime.UtcNow < reopenAt && await timer.WaitForNextTickAsync(cancellationToken));

        await call.RequestStream.CompleteAsync();
        await call.ResponseAsync;
    }
}
