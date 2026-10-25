using FakeGameServer;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Shared.Communication;
using Shared.Grpc.Session;

// Stand-in for the Unity game server in local development and integration tests:
// registers in SessionService, keeps heartbeats, admits players by join ticket and reports matches.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FakeGameServerOptions>(builder.Configuration.GetSection(FakeGameServerOptions.SECTION_NAME));
builder.Services.AddServiceTokenProvider(builder.Configuration);
builder.Services.AddSingleton(serviceProvider =>
{
    string address = serviceProvider.GetRequiredService<IOptions<FakeGameServerOptions>>().Value.SessionGrpcAddress;
    return new GameServerSessions.GameServerSessionsClient(GrpcChannel.ForAddress(address));
});
builder.Services.AddSingleton<SessionClient>();
builder.Services.AddSingleton<ServerState>();
builder.Services.AddHostedService<HeartbeatWorker>();

WebApplication app = builder.Build();

app.MapGet("/", (ServerState state) => Results.Ok(new
{
    state.ServerId,
    Sessions = state.Sessions.Values.Select(session => new
    {
        session.Id,
        session.Kind,
        session.Mode,
        session.MatchId,
        Players = session.Players.Keys,
    }),
}));

// What a game client does after POST /sessions/join: connect to the server with the ticket.
app.MapPost("/connect", async (ConnectRequest request, SessionClient client, ServerState state, CancellationToken ct) =>
{
    ValidateJoinTicketReply validation = await client.UnaryAsync(
        options => client.Raw.ValidateJoinTicketAsync(
            new ValidateJoinTicketRequest { ServerId = state.ServerId, Ticket = request.Ticket },
            options),
        ct);

    if (!validation.Valid)
    {
        return Results.BadRequest(new { validation.ErrorCode, validation.ErrorMessage });
    }

    Guid sessionId = Guid.Parse(validation.SessionId);
    Guid accountId = Guid.Parse(validation.AccountId);

    PlayerEventReply joined = await client.UnaryAsync(
        options => client.Raw.PlayerJoinedAsync(
            new PlayerJoinedRequest { ServerId = state.ServerId, SessionId = validation.SessionId, AccountId = validation.AccountId },
            options),
        ct);

    if (!joined.Success)
    {
        return Results.Conflict(new { joined.ErrorCode, joined.ErrorMessage });
    }

    FakeSession session = state.Sessions.GetOrAdd(sessionId, id => new FakeSession(
        id,
        validation.Kind,
        validation.Mode,
        Guid.TryParse(validation.MatchId, out Guid matchId) ? matchId : null,
        DateTime.UtcNow));

    session.Players[accountId] = DateTime.UtcNow;
    session.LastActivityAt = DateTime.UtcNow;

    return Results.Ok(new { SessionId = sessionId, AccountId = accountId, validation.Kind, validation.Mode });
});

app.MapPost("/sessions/{sessionId:guid}/leave/{accountId:guid}", async (
    Guid sessionId,
    Guid accountId,
    SessionClient client,
    ServerState state,
    CancellationToken ct) =>
{
    PlayerEventReply reply = await client.UnaryAsync(
        options => client.Raw.PlayerLeftAsync(
            new PlayerLeftRequest { ServerId = state.ServerId, SessionId = sessionId.ToString(), AccountId = accountId.ToString() },
            options),
        ct);

    if (state.Sessions.TryGetValue(sessionId, out FakeSession? session))
    {
        session.Players.TryRemove(accountId, out _);
        session.LastActivityAt = DateTime.UtcNow;
    }

    return reply.Success ? Results.Ok(reply) : Results.BadRequest(reply);
});

// Reports a match between the players currently in the session: two teams, random scores.
// Pass the same activityId again to check that a retried report is published only once.
app.MapPost("/sessions/{sessionId:guid}/report", async (
    Guid sessionId,
    ReportRequest? request,
    SessionClient client,
    ServerState state,
    CancellationToken ct) =>
{
    if (!state.Sessions.TryGetValue(sessionId, out FakeSession? session) || session.Players.IsEmpty)
    {
        return Results.NotFound(new { Error = "No players in this session" });
    }

    DateTime endedAt = DateTime.UtcNow;
    DateTime startedAt = endedAt.AddSeconds(-(request?.DurationSeconds ?? 300));
    string activityId = string.IsNullOrWhiteSpace(request?.ActivityId) ? Guid.CreateVersion7().ToString() : request.ActivityId;

    var report = new ReportMatchRequest
    {
        ServerId = state.ServerId,
        SessionId = sessionId.ToString(),
        ActivityId = activityId,
        Mode = session.Mode,
        StartedAt = Timestamp.FromDateTime(startedAt),
        EndedAt = Timestamp.FromDateTime(endedAt),
        StatsJson = """{"source":"fake-game-server"}""",
    };

    int index = 0;
    foreach (Guid accountId in session.Players.Keys.Order())
    {
        int team = (index++ % 2) + 1;
        report.Players.Add(new PlayerResult
        {
            AccountId = accountId.ToString(),
            Team = team,
            Score = team == 1 ? 150 : 100,
            Kills = team == 1 ? 7 : 4,
            Deaths = team == 1 ? 4 : 7,
            IsWinner = team == 1,
        });
    }

    ReportMatchReply reply = await client.UnaryAsync(options => client.Raw.ReportMatchAsync(report, options), ct);
    session.LastActivityAt = DateTime.UtcNow;

    return reply.Accepted
        ? Results.Ok(new { ActivityId = activityId, reply.Duplicate, reply.Suspicious })
        : Results.BadRequest(new { ActivityId = activityId, reply.ErrorCode, reply.ErrorMessage });
});

await app.RunAsync();
