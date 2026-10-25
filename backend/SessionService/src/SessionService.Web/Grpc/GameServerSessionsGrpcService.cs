using CSharpFunctionalExtensions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SessionService.Core.GameServers;
using SessionService.Domain;
using SessionService.Domain.Reports;
using SessionService.Domain.Sessions;
using Shared.Grpc.Session;
using Shared.SharedKernel.Errors;

namespace SessionService.Web.Grpc;

/// <summary>
/// Translates the game server protocol to <see cref="GameServerOperations"/>.
/// Business errors go back in the reply; transient failures become <c>Unavailable</c> so the
/// server retries (reports are idempotent by activity id).
/// </summary>
public sealed class GameServerSessionsGrpcService(
    GameServerOperations operations,
    IServiceScopeFactory scopeFactory,
    ILogger<GameServerSessionsGrpcService> logger) : GameServerSessions.GameServerSessionsBase
{
    private const string INVALID_REQUEST = "session.invalid_request";

    public override async Task<RegisterReply> Register(RegisterRequest request, ServerCallContext context)
    {
        Guid? serverId = null;
        if (!string.IsNullOrWhiteSpace(request.ServerId))
        {
            if (!Guid.TryParse(request.ServerId, out Guid parsed))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "server_id must be a GUID"));
            }

            serverId = parsed;
        }

        var result = await operations.RegisterAsync(
            serverId,
            request.Address,
            request.Region,
            [.. request.Modes],
            request.Capacity,
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw ToRpcException(result.Error);
        }

        return new RegisterReply
        {
            ServerId = result.Value.ServerId.ToString(),
            HeartbeatIntervalSeconds = result.Value.HeartbeatIntervalSeconds,
            TicketPublicKeyPem = result.Value.TicketPublicKeyPem,
            TicketKeyId = result.Value.TicketKeyId,
        };
    }

    public override async Task<HeartbeatReply> Heartbeat(
        IAsyncStreamReader<HeartbeatRequest> requestStream,
        ServerCallContext context)
    {
        int received = 0;

        await foreach (HeartbeatRequest message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (!Guid.TryParse(message.ServerId, out Guid serverId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "server_id must be a GUID"));
            }

            Guid[] sessionIds = [.. message.SessionIds
                .Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)];

            // One scope per message: the stream may stay open for hours.
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var scopedOperations = scope.ServiceProvider.GetRequiredService<GameServerOperations>();

            var result = await scopedOperations.HeartbeatAsync(serverId, sessionIds, context.CancellationToken);
            if (result.IsFailure)
            {
                throw ToRpcException(result.Error);
            }

            received++;
        }

        return new HeartbeatReply { Received = received };
    }

    public override async Task<ValidateJoinTicketReply> ValidateJoinTicket(
        ValidateJoinTicketRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ServerId, out Guid serverId))
        {
            return new ValidateJoinTicketReply { Valid = false, ErrorCode = INVALID_REQUEST, ErrorMessage = "server_id must be a GUID" };
        }

        var result = await operations.ValidateTicketAsync(serverId, request.Ticket, context.CancellationToken);
        if (result.IsFailure)
        {
            ThrowIfTransient(result.Error);
            (string code, string message) = Describe(result.Error);
            return new ValidateJoinTicketReply { Valid = false, ErrorCode = code, ErrorMessage = message };
        }

        Session session = result.Value.Session;

        return new ValidateJoinTicketReply
        {
            Valid = true,
            AccountId = result.Value.AccountId.ToString(),
            SessionId = session.Id.ToString(),
            Kind = session.Kind.ToWireName(),
            Mode = session.Mode,
            MatchId = session.MatchId?.ToString() ?? string.Empty,
        };
    }

    public override async Task<PlayerEventReply> PlayerJoined(PlayerJoinedRequest request, ServerCallContext context)
    {
        if (!TryParseIds(request.ServerId, request.SessionId, request.AccountId, out var ids))
        {
            return InvalidPlayerEvent();
        }

        return ToPlayerEventReply(await operations.PlayerJoinedAsync(
            ids.ServerId,
            ids.SessionId,
            ids.AccountId,
            context.CancellationToken));
    }

    public override async Task<PlayerEventReply> PlayerLeft(PlayerLeftRequest request, ServerCallContext context)
    {
        if (!TryParseIds(request.ServerId, request.SessionId, request.AccountId, out var ids))
        {
            return InvalidPlayerEvent();
        }

        return ToPlayerEventReply(await operations.PlayerLeftAsync(
            ids.ServerId,
            ids.SessionId,
            ids.AccountId,
            context.CancellationToken));
    }

    public override async Task<ReportMatchReply> ReportMatch(ReportMatchRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ServerId, out Guid serverId)
            || !Guid.TryParse(request.SessionId, out Guid sessionId)
            || request.StartedAt is null
            || request.EndedAt is null)
        {
            return new ReportMatchReply
            {
                Accepted = false,
                ErrorCode = INVALID_REQUEST,
                ErrorMessage = "server_id, session_id, started_at and ended_at are required",
            };
        }

        var players = new List<ReportedPlayer>(request.Players.Count);
        foreach (PlayerResult player in request.Players)
        {
            if (!Guid.TryParse(player.AccountId, out Guid accountId))
            {
                return new ReportMatchReply { Accepted = false, ErrorCode = INVALID_REQUEST, ErrorMessage = "account_id must be a GUID" };
            }

            players.Add(new ReportedPlayer(accountId, player.Team, player.Score, player.Kills, player.Deaths, player.IsWinner));
        }

        var report = new MatchReport(
            request.ActivityId,
            request.Mode,
            ToUtc(request.StartedAt),
            ToUtc(request.EndedAt),
            players,
            string.IsNullOrWhiteSpace(request.StatsJson) ? null : request.StatsJson);

        var result = await operations.ReportMatchAsync(serverId, sessionId, report, context.CancellationToken);
        if (result.IsFailure)
        {
            ThrowIfTransient(result.Error);
            (string code, string message) = Describe(result.Error);
            return new ReportMatchReply { Accepted = false, ErrorCode = code, ErrorMessage = message };
        }

        return new ReportMatchReply
        {
            Accepted = true,
            Duplicate = result.Value.Duplicate,
            Suspicious = result.Value.Suspicious,
        };
    }

    private static DateTime ToUtc(Timestamp timestamp) => timestamp.ToDateTime();

    private static bool TryParseIds(
        string serverId,
        string sessionId,
        string accountId,
        out (Guid ServerId, Guid SessionId, Guid AccountId) ids)
    {
        bool serverValid = Guid.TryParse(serverId, out Guid server);
        bool sessionValid = Guid.TryParse(sessionId, out Guid session);
        bool accountValid = Guid.TryParse(accountId, out Guid account);

        ids = (server, session, account);
        return serverValid && sessionValid && accountValid;
    }

    private PlayerEventReply ToPlayerEventReply(UnitResult<Error> result)
    {
        if (result.IsSuccess)
        {
            return new PlayerEventReply { Success = true };
        }

        ThrowIfTransient(result.Error);
        (string code, string message) = Describe(result.Error);
        return new PlayerEventReply { Success = false, ErrorCode = code, ErrorMessage = message };
    }

    private static PlayerEventReply InvalidPlayerEvent() =>
        new() { Success = false, ErrorCode = INVALID_REQUEST, ErrorMessage = "server_id, session_id and account_id must be GUIDs" };

    private void ThrowIfTransient(Error error)
    {
        if (error.Behavior == ErrorBehavior.Transient || error.Type == ErrorType.FAILURE)
        {
            logger.LogWarning("Game server call failed: {Error}", error.GetMessage());
            throw new RpcException(new Status(StatusCode.Unavailable, "Temporary failure, retry the call"));
        }
    }

    private RpcException ToRpcException(Error error)
    {
        ThrowIfTransient(error);

        StatusCode code = error.Type switch
        {
            ErrorType.NOT_FOUND => StatusCode.NotFound,
            ErrorType.AUTHORIZATION => StatusCode.PermissionDenied,
            ErrorType.AUTHENTICATION => StatusCode.Unauthenticated,
            ErrorType.CONFLICT => StatusCode.FailedPrecondition,
            _ => StatusCode.InvalidArgument,
        };

        return new RpcException(new Status(code, Describe(error).Message));
    }

    private static (string Code, string Message) Describe(Error error) =>
        error.Messages.Count == 0
            ? ("session.failed", "Session operation failed")
            : (error.Messages[0].Code, string.Join("; ", error.Messages.Select(message => message.Message)));
}
