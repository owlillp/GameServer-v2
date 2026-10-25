using Microsoft.AspNetCore.Mvc;
using SessionService.Contracts.Dtos;
using SessionService.Core.Features.Admin.Queries.ListServers;
using SessionService.Core.Features.Admin.Queries.ListSessions;
using SessionService.Domain.Authorization;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace SessionService.Web.Endpoints.Admin;

public sealed class AdminSessionEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/sessions";
    private const int DEFAULT_LIMIT = 100;

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Sessions")
            .RequireAuthorization();

        group.MapGet("/", ListSessions)
            .WithName("Admin.Sessions.List")
            .WithSummary("List sessions, newest first")
            .RequirePermissions(SessionPermissions.Sessions.VIEW);

        group.MapGet("/servers", ListServers)
            .WithName("Admin.Sessions.Servers")
            .WithSummary("List registered game servers")
            .RequirePermissions(SessionPermissions.Sessions.VIEW);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<SessionDto>>> ListSessions(
        [FromQuery] bool? openOnly,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<SessionDto>, ListSessionsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListSessionsQuery(openOnly ?? true, limit ?? DEFAULT_LIMIT), ct);

    private static async Task<EndpointResult<IReadOnlyList<GameServerDto>>> ListServers(
        [FromQuery] string? region,
        [FromServices] IQueryHandler<IReadOnlyList<GameServerDto>, ListServersQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListServersQuery(region), ct);
}
