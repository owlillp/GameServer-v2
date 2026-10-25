using ClanService.Contracts.Dtos;
using ClanService.Contracts.Requests;
using ClanService.Core.Features.Admin;
using ClanService.Core.Features.Clans.Queries;
using ClanService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace ClanService.Web.Endpoints.Admin;

public sealed class AdminClanEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/clans";
    private const int DEFAULT_LEDGER_LIMIT = 100;

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Clans")
            .RequireAuthorization();

        group.MapGet("/", Search)
            .WithName("Admin.Clans.Search")
            .WithSummary("Search clans, including disbanded")
            .RequirePermissions(ClanPermissions.Clans.VIEW);

        group.MapGet("/{clanId:guid}", Get)
            .WithName("Admin.Clans.Get")
            .WithSummary("Clan with its roster")
            .RequirePermissions(ClanPermissions.Clans.VIEW);

        group.MapGet("/{clanId:guid}/points", Ledger)
            .WithName("Admin.Clans.Points.Ledger")
            .WithSummary("Clan points ledger, newest first")
            .RequirePermissions(ClanPermissions.Clans.VIEW);

        group.MapPost("/{clanId:guid}/points", Adjust)
            .WithName("Admin.Clans.Points.Adjust")
            .WithSummary("Correct clan points; the ledger records who and why")
            .RequirePermissions(ClanPermissions.Clans.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<ClanDto>>> Search(
        [FromQuery] string? query,
        [FromServices] IQueryHandler<IReadOnlyList<ClanDto>, SearchClansQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new SearchClansQuery(query, ActiveOnly: false), ct);

    private static async Task<EndpointResult<ClanDetailsDto>> Get(
        [FromRoute] Guid clanId,
        [FromServices] IQueryHandler<ClanDetailsDto, GetClanQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetClanQuery(clanId), ct);

    private static async Task<EndpointResult<IReadOnlyList<ClanPointsEntryDto>>> Ledger(
        [FromRoute] Guid clanId,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<ClanPointsEntryDto>, GetPointsLedgerQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetPointsLedgerQuery(clanId, limit ?? DEFAULT_LEDGER_LIMIT), ct);

    private static async Task<EndpointResult<ClanPointsEntryDto>> Adjust(
        [FromRoute] Guid clanId,
        [FromBody] AdjustPointsRequest request,
        [FromServices] ICommandHandler<ClanPointsEntryDto, AdjustPointsCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(
            new AdjustPointsCommand(userScope.Id ?? Guid.Empty, clanId, request.Amount, request.Reason, request.Comment, request.OperationId),
            ct);
}
