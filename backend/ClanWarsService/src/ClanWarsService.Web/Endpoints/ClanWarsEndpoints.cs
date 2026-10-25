using ClanWarsService.Contracts;
using ClanWarsService.Core.Features;
using ClanWarsService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace ClanWarsService.Web.Endpoints;

public sealed class ClanWarsEndpoints : IEndpointGroup
{
    private const int DEFAULT_LIMIT = 100;
    private const int DEFAULT_NEIGHBORS = 5;

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/clanwars").WithTags("ClanWars").RequireAuthorization();

        group.MapGet("/seasons/current", GetCurrent).WithName("ClanWars.Seasons.Current").WithSummary("Running season, or the next scheduled one");
        group.MapGet("/seasons/{seasonId:guid}/leaderboard", GetLeaderboard).WithName("ClanWars.Leaderboard").WithSummary("Top clans of a season");
        group.MapGet("/seasons/{seasonId:guid}/leaderboard/me", GetMine).WithName("ClanWars.Leaderboard.Me").WithSummary("The player's clan place with neighbours");
        group.MapGet("/seasons/{seasonId:guid}/clans/{clanId:guid}", GetClan).WithName("ClanWars.Leaderboard.Clan").WithSummary("A clan's place with neighbours");
        group.MapGet("/seasons/{seasonId:guid}/clans/{clanId:guid}/members", GetMembers).WithName("ClanWars.Clan.Members").WithSummary("Member contributions in a season");

        return group;
    }

    private static async Task<EndpointResult<SeasonDto>> GetCurrent(
        [FromServices] IQueryHandler<SeasonDto, GetCurrentSeasonQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetCurrentSeasonQuery(), ct);

    private static async Task<EndpointResult<IReadOnlyList<LeaderboardRowDto>>> GetLeaderboard(
        [FromRoute] Guid seasonId,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<LeaderboardRowDto>, GetLeaderboardQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetLeaderboardQuery(seasonId, limit ?? DEFAULT_LIMIT), ct);

    private static async Task<EndpointResult<ClanStandingDto>> GetMine(
        [FromRoute] Guid seasonId,
        [FromQuery] int? neighbors,
        [FromServices] IQueryHandler<ClanStandingDto, GetMyStandingQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyStandingQuery(seasonId, userScope.Id ?? Guid.Empty, neighbors ?? DEFAULT_NEIGHBORS), ct);

    private static async Task<EndpointResult<ClanStandingDto>> GetClan(
        [FromRoute] Guid seasonId,
        [FromRoute] Guid clanId,
        [FromQuery] int? neighbors,
        [FromServices] IQueryHandler<ClanStandingDto, GetClanStandingQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetClanStandingQuery(seasonId, clanId, neighbors ?? DEFAULT_NEIGHBORS), ct);

    private static async Task<EndpointResult<IReadOnlyList<MemberContributionDto>>> GetMembers(
        [FromRoute] Guid seasonId,
        [FromRoute] Guid clanId,
        [FromServices] IQueryHandler<IReadOnlyList<MemberContributionDto>, GetMemberContributionsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetMemberContributionsQuery(seasonId, clanId), ct);
}

public sealed class AdminClanWarsEndpoints : IEndpointGroup
{
    private const int DEFAULT_RECEIPTS = 100;

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin/clanwars").WithTags("Admin.ClanWars").RequireAuthorization();

        group.MapGet("/seasons", List).WithName("Admin.ClanWars.Seasons.List").WithSummary("All seasons").RequirePermissions(ClanWarsPermissions.ClanWars.VIEW);
        group.MapPost("/seasons", Create).WithName("Admin.ClanWars.Seasons.Create").WithSummary("Create a draft season").RequirePermissions(ClanWarsPermissions.ClanWars.MANAGE);
        group.MapPut("/seasons/{seasonId:guid}", Update).WithName("Admin.ClanWars.Seasons.Update").WithSummary("Change a draft season").RequirePermissions(ClanWarsPermissions.ClanWars.MANAGE);
        group.MapPost("/seasons/{seasonId:guid}/publish", Publish).WithName("Admin.ClanWars.Seasons.Publish").WithSummary("Publish: phases follow the schedule from now on").RequirePermissions(ClanWarsPermissions.ClanWars.MANAGE);
        group.MapGet("/seasons/{seasonId:guid}/scores", Receipts).WithName("Admin.ClanWars.Scores.List").WithSummary("Score receipts (audit)").RequirePermissions(ClanWarsPermissions.ClanWars.VIEW);
        group.MapPost("/seasons/{seasonId:guid}/scores", Adjust).WithName("Admin.ClanWars.Scores.Adjust").WithSummary("Correct a clan's season points").RequirePermissions(ClanWarsPermissions.ClanWars.MANAGE);
        group.MapPost("/seasons/{seasonId:guid}/leaderboard/rebuild", Rebuild).WithName("Admin.ClanWars.Leaderboard.Rebuild").WithSummary("Rebuild the Redis leaderboard from PostgreSQL").RequirePermissions(ClanWarsPermissions.ClanWars.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<SeasonDto>>> List(
        [FromServices] IQueryHandler<IReadOnlyList<SeasonDto>, ListSeasonsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListSeasonsQuery(), ct);

    private static async Task<EndpointResult<SeasonDto>> Create(
        [FromBody] CreateSeasonRequest request,
        [FromServices] ICommandHandler<SeasonDto, CreateSeasonCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateSeasonCommand(request), ct);

    private static async Task<EndpointResult<SeasonDto>> Update(
        [FromRoute] Guid seasonId,
        [FromBody] CreateSeasonRequest request,
        [FromServices] ICommandHandler<SeasonDto, UpdateSeasonCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateSeasonCommand(seasonId, request), ct);

    private static async Task<EndpointResult<SeasonDto>> Publish(
        [FromRoute] Guid seasonId,
        [FromServices] ICommandHandler<SeasonDto, PublishSeasonCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new PublishSeasonCommand(seasonId), ct);

    private static async Task<EndpointResult<IReadOnlyList<ScoreReceiptDto>>> Receipts(
        [FromRoute] Guid seasonId,
        [FromQuery] Guid? clanId,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<ScoreReceiptDto>, ListScoreReceiptsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListScoreReceiptsQuery(seasonId, clanId, limit ?? DEFAULT_RECEIPTS), ct);

    private static async Task<EndpointResult<ScoreReceiptDto>> Adjust(
        [FromRoute] Guid seasonId,
        [FromBody] AdjustScoreRequest request,
        [FromServices] ICommandHandler<ScoreReceiptDto, AdjustScoreCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new AdjustScoreCommand(userScope.Id ?? Guid.Empty, seasonId, request), ct);

    private static async Task<EndpointResult<bool>> Rebuild(
        [FromRoute] Guid seasonId,
        [FromServices] ICommandHandler<bool, RebuildLeaderboardCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new RebuildLeaderboardCommand(seasonId), ct);
}
