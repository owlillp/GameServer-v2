using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Contracts.Requests;
using MatchmakingService.Core.Features.Leaderboard.Commands.RebuildLeaderboard;
using MatchmakingService.Core.Features.Matches.Commands.CancelMatch;
using MatchmakingService.Core.Features.Matches.Commands.CreateMatch;
using MatchmakingService.Core.Features.Matches.Commands.FinishMatch;
using MatchmakingService.Core.Features.Matches.Commands.SubmitResults;
using MatchmakingService.Core.Features.Matches.Queries.GetAdminMatch;
using MatchmakingService.Core.Features.Matches.Queries.ListMatches;
using MatchmakingService.Core.Features.Parties.Queries.GetParty;
using MatchmakingService.Core.Features.Queues.Queries.ListQueue;
using MatchmakingService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace MatchmakingService.Web.Endpoints.Admin;

public sealed class AdminMatchmakingEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/matchmaking";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Matchmaking")
            .RequireAuthorization();

        group.MapGet("/matches", ListMatches)
            .WithName("Admin.Matchmaking.Matches.List")
            .WithSummary("List matches")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.VIEW);

        group.MapGet("/matches/{matchId:guid}", GetMatch)
            .WithName("Admin.Matchmaking.Matches.Get")
            .WithSummary("Get a match with reward grants")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.VIEW);

        group.MapPost("/matches", CreateMatch)
            .WithName("Admin.Matchmaking.Matches.Create")
            .WithSummary("Create a match from two parties")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.MANAGE);

        group.MapPut("/matches/{matchId:guid}/results", SubmitResults)
            .WithName("Admin.Matchmaking.Matches.Results")
            .WithSummary("Submit or update match results")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.MANAGE);

        group.MapPost("/matches/{matchId:guid}/finish", FinishMatch)
            .WithName("Admin.Matchmaking.Matches.Finish")
            .WithSummary("Finish a match and grant rewards")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.MANAGE);

        group.MapPost("/matches/{matchId:guid}/cancel", CancelMatch)
            .WithName("Admin.Matchmaking.Matches.Cancel")
            .WithSummary("Cancel a match")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.MANAGE);

        group.MapGet("/queue", ListQueue)
            .WithName("Admin.Matchmaking.Queue.List")
            .WithSummary("List queued parties")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.VIEW);

        group.MapGet("/parties/{partyId:guid}", GetParty)
            .WithName("Admin.Matchmaking.Parties.Get")
            .WithSummary("Get a party by id")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.VIEW);

        group.MapGet("/parties/by-code/{code}", GetPartyByCode)
            .WithName("Admin.Matchmaking.Parties.ByCode")
            .WithSummary("Get a party by invite code")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.VIEW);

        group.MapPost("/leaderboard/rebuild", RebuildLeaderboard)
            .WithName("Admin.Matchmaking.Leaderboard.Rebuild")
            .WithSummary("Rebuild the leaderboard cache from PostgreSQL")
            .RequirePermissions(MatchmakingPermissions.Matchmaking.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<MatchDetailsDto>>> ListMatches(
        [FromQuery] string? status,
        [FromQuery] int limit,
        [FromServices] IQueryHandler<IReadOnlyList<MatchDetailsDto>, ListMatchesQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListMatchesQuery(status, limit <= 0 ? 50 : limit), ct);

    private static async Task<EndpointResult<AdminMatchDto>> GetMatch(
        [FromRoute] Guid matchId,
        [FromServices] IQueryHandler<AdminMatchDto, GetAdminMatchQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetAdminMatchQuery(matchId), ct);

    private static async Task<EndpointResult<MatchDetailsDto>> CreateMatch(
        [FromBody] CreateMatchRequest request,
        [FromServices] ICommandHandler<MatchDetailsDto, CreateMatchCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateMatchCommand(request), ct);

    private static async Task<EndpointResult<MatchDetailsDto>> SubmitResults(
        [FromRoute] Guid matchId,
        [FromBody] SubmitMatchResultsRequest request,
        [FromServices] ICommandHandler<MatchDetailsDto, SubmitResultsCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new SubmitResultsCommand(matchId, request.Results), ct);

    private static async Task<EndpointResult<AdminMatchDto>> FinishMatch(
        [FromRoute] Guid matchId,
        [FromServices] ICommandHandler<AdminMatchDto, FinishMatchCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new FinishMatchCommand(matchId), ct);

    private static async Task<EndpointResult<MatchDetailsDto>> CancelMatch(
        [FromRoute] Guid matchId,
        [FromServices] ICommandHandler<MatchDetailsDto, CancelMatchCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CancelMatchCommand(matchId), ct);

    private static async Task<EndpointResult<IReadOnlyList<QueueTicketDto>>> ListQueue(
        [FromQuery] int limit,
        [FromServices] IQueryHandler<IReadOnlyList<QueueTicketDto>, ListQueueQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListQueueQuery(limit <= 0 ? 100 : limit), ct);

    private static async Task<EndpointResult<PartyDto>> GetParty(
        [FromRoute] Guid partyId,
        [FromServices] IQueryHandler<PartyDto, GetPartyQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetPartyQuery(partyId, null), ct);

    private static async Task<EndpointResult<PartyDto>> GetPartyByCode(
        [FromRoute] string code,
        [FromServices] IQueryHandler<PartyDto, GetPartyQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetPartyQuery(null, code), ct);

    private static async Task<EndpointResult<bool>> RebuildLeaderboard(
        [FromServices] ICommandHandler<bool, RebuildLeaderboardCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new RebuildLeaderboardCommand(), ct);
}
