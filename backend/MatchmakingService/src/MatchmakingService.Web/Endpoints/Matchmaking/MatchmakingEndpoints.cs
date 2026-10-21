using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Contracts.Requests;
using MatchmakingService.Core.Features.Leaderboard.Queries.GetLeaderboard;
using MatchmakingService.Core.Features.Leaderboard.Queries.GetMyLeaderboard;
using MatchmakingService.Core.Features.Matches.Queries.GetMatch;
using MatchmakingService.Core.Features.Matches.Queries.GetMyMatch;
using MatchmakingService.Core.Features.Parties.Commands.CreateParty;
using MatchmakingService.Core.Features.Parties.Commands.JoinParty;
using MatchmakingService.Core.Features.Parties.Commands.KickMember;
using MatchmakingService.Core.Features.Parties.Commands.LeaveParty;
using MatchmakingService.Core.Features.Parties.Commands.SetReady;
using MatchmakingService.Core.Features.Parties.Queries.GetMyParty;
using MatchmakingService.Core.Features.Queues.Commands.EnqueueParty;
using MatchmakingService.Core.Features.Queues.Commands.LeaveQueue;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace MatchmakingService.Web.Endpoints.Matchmaking;

public sealed class MatchmakingEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/matchmaking";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Matchmaking")
            .RequireAuthorization();

        group.MapPost("/parties", CreateParty)
            .WithName("Matchmaking.Parties.Create")
            .WithSummary("Create a party");

        group.MapGet("/parties/me", GetMyParty)
            .WithName("Matchmaking.Parties.Me")
            .WithSummary("Get the current player's party, queue ticket and active match");

        group.MapPost("/parties/join", JoinParty)
            .WithName("Matchmaking.Parties.Join")
            .WithSummary("Join a party by invite code");

        group.MapPost("/parties/leave", LeaveParty)
            .WithName("Matchmaking.Parties.Leave")
            .WithSummary("Leave the current party");

        group.MapPost("/parties/kick/{accountId:guid}", KickMember)
            .WithName("Matchmaking.Parties.Kick")
            .WithSummary("Kick a party member (leader only)");

        group.MapPut("/parties/ready", SetReady)
            .WithName("Matchmaking.Parties.Ready")
            .WithSummary("Set ready state for the current player");

        group.MapPost("/queue", EnqueueParty)
            .WithName("Matchmaking.Queue.Enqueue")
            .WithSummary("Enqueue the current party");

        group.MapDelete("/queue", LeaveQueue)
            .WithName("Matchmaking.Queue.Leave")
            .WithSummary("Leave the queue");

        group.MapGet("/matches/me", GetMyMatch)
            .WithName("Matchmaking.Matches.Me")
            .WithSummary("Get the current player's active match");

        group.MapGet("/matches/{matchId:guid}", GetMatch)
            .WithName("Matchmaking.Matches.Get")
            .WithSummary("Get an active match by id");

        group.MapGet("/leaderboard", GetLeaderboard)
            .WithName("Matchmaking.Leaderboard.List")
            .WithSummary("Get the global leaderboard");

        group.MapGet("/leaderboard/me", GetMyLeaderboard)
            .WithName("Matchmaking.Leaderboard.Me")
            .WithSummary("Get the current player's leaderboard position");

        return group;
    }

    private static async Task<EndpointResult<PartyDto>> CreateParty(
        [FromServices] ICommandHandler<PartyDto, CreatePartyCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new CreatePartyCommand(userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<PartyStateDto>> GetMyParty(
        [FromServices] IQueryHandler<PartyStateDto, GetMyPartyQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyPartyQuery(userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<PartyDto>> JoinParty(
        [FromBody] JoinPartyRequest request,
        [FromServices] ICommandHandler<PartyDto, JoinPartyCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new JoinPartyCommand(userScope.Id ?? Guid.Empty, request.Code), ct);

    private static async Task<EndpointResult<bool>> LeaveParty(
        [FromServices] ICommandHandler<bool, LeavePartyCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new LeavePartyCommand(userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<PartyDto>> KickMember(
        [FromRoute] Guid accountId,
        [FromServices] ICommandHandler<PartyDto, KickMemberCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new KickMemberCommand(userScope.Id ?? Guid.Empty, accountId), ct);

    private static async Task<EndpointResult<PartyDto>> SetReady(
        [FromBody] SetPartyReadyRequest request,
        [FromServices] ICommandHandler<PartyDto, SetReadyCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new SetReadyCommand(userScope.Id ?? Guid.Empty, request.IsReady), ct);

    private static async Task<EndpointResult<QueueTicketDto>> EnqueueParty(
        [FromBody] EnqueuePartyRequest? request,
        [FromServices] ICommandHandler<QueueTicketDto, EnqueuePartyCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new EnqueuePartyCommand(userScope.Id ?? Guid.Empty, request?.Mode), ct);

    private static async Task<EndpointResult<bool>> LeaveQueue(
        [FromServices] ICommandHandler<bool, LeaveQueueCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new LeaveQueueCommand(userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<MatchDetailsDto?>> GetMyMatch(
        [FromServices] IQueryHandler<MatchDetailsDto?, GetMyMatchQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyMatchQuery(userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<MatchDetailsDto>> GetMatch(
        [FromRoute] Guid matchId,
        [FromServices] IQueryHandler<MatchDetailsDto, GetMatchQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMatchQuery(matchId, userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<IReadOnlyList<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] int limit,
        [FromServices] IQueryHandler<IReadOnlyList<LeaderboardEntryDto>, GetLeaderboardQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetLeaderboardQuery(limit <= 0 ? 50 : limit), ct);

    private static async Task<EndpointResult<MyLeaderboardDto>> GetMyLeaderboard(
        [FromServices] IQueryHandler<MyLeaderboardDto, GetMyLeaderboardQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyLeaderboardQuery(userScope.Id ?? Guid.Empty), ct);
}
