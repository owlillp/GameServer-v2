using ClanService.Contracts.Dtos;
using ClanService.Contracts.Requests;
using ClanService.Core.Features.Clans.Commands;
using ClanService.Core.Features.Clans.Queries;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace ClanService.Web.Endpoints.Clans;

public sealed class ClanEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/clans";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Clans")
            .RequireAuthorization();

        group.MapPost("/", Create).WithName("Clans.Create").WithSummary("Create a clan (the player becomes its leader)");
        group.MapGet("/", Search).WithName("Clans.Search").WithSummary("Search clans by tag prefix or name");
        group.MapGet("/me", GetMine).WithName("Clans.Me").WithSummary("The player's clan, role and roster");
        group.MapPost("/me/leave", Leave).WithName("Clans.Leave").WithSummary("Leave the clan (a leader alone disbands it)");
        group.MapGet("/requests/me", ListMyRequests).WithName("Clans.Requests.Me").WithSummary("Pending invites and requests of the player");
        group.MapPost("/requests/{requestId:guid}/accept", Accept).WithName("Clans.Requests.Accept").WithSummary("Accept an invite or a join request");
        group.MapPost("/requests/{requestId:guid}/decline", Decline).WithName("Clans.Requests.Decline").WithSummary("Decline an invite or a request, or withdraw one's own request");
        group.MapGet("/{clanId:guid}", Get).WithName("Clans.Get").WithSummary("Clan with its roster");
        group.MapPut("/{clanId:guid}", Update).WithName("Clans.Update").WithSummary("Change name and description (officer+)");
        group.MapDelete("/{clanId:guid}", Disband).WithName("Clans.Disband").WithSummary("Disband the clan (leader)");
        group.MapPost("/{clanId:guid}/requests", RequestJoin).WithName("Clans.Requests.Create").WithSummary("Ask to join a clan");
        group.MapGet("/{clanId:guid}/requests", ListRequests).WithName("Clans.Requests.List").WithSummary("Pending requests and invites (officer+)");
        group.MapPost("/{clanId:guid}/invites", Invite).WithName("Clans.Invites.Create").WithSummary("Invite a player (officer+)");
        group.MapPut("/{clanId:guid}/members/{accountId:guid}/role", ChangeRole).WithName("Clans.Members.Role").WithSummary("Promote or demote a member (leader)");
        group.MapDelete("/{clanId:guid}/members/{accountId:guid}", Kick).WithName("Clans.Members.Kick").WithSummary("Kick a member of a lower role");
        group.MapPost("/{clanId:guid}/transfer-leadership", Transfer).WithName("Clans.TransferLeadership").WithSummary("Hand leadership to another member (leader)");

        return group;
    }

    private static Guid Me(UserScopedData userScope) => userScope.Id ?? Guid.Empty;

    private static async Task<EndpointResult<MyClanDto>> Create(
        [FromBody] CreateClanRequest request,
        [FromServices] ICommandHandler<MyClanDto, CreateClanCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new CreateClanCommand(Me(userScope), request.Tag, request.Name, request.Description), ct);

    private static async Task<EndpointResult<IReadOnlyList<ClanDto>>> Search(
        [FromQuery] string? query,
        [FromServices] IQueryHandler<IReadOnlyList<ClanDto>, SearchClansQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new SearchClansQuery(query, ActiveOnly: true), ct);

    private static async Task<EndpointResult<MyClanDto>> GetMine(
        [FromServices] IQueryHandler<MyClanDto, GetMyClanQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyClanQuery(Me(userScope)), ct);

    private static async Task<EndpointResult<bool>> Leave(
        [FromServices] ICommandHandler<bool, LeaveClanCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new LeaveClanCommand(Me(userScope)), ct);

    private static async Task<EndpointResult<IReadOnlyList<ClanJoinRequestDto>>> ListMyRequests(
        [FromServices] IQueryHandler<IReadOnlyList<ClanJoinRequestDto>, ListMyRequestsQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new ListMyRequestsQuery(Me(userScope)), ct);

    private static async Task<EndpointResult<ClanMemberDto>> Accept(
        [FromRoute] Guid requestId,
        [FromServices] ICommandHandler<ClanMemberDto, AcceptRequestCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new AcceptRequestCommand(Me(userScope), requestId), ct);

    private static async Task<EndpointResult<bool>> Decline(
        [FromRoute] Guid requestId,
        [FromServices] ICommandHandler<bool, DeclineRequestCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new DeclineRequestCommand(Me(userScope), requestId), ct);

    private static async Task<EndpointResult<ClanDetailsDto>> Get(
        [FromRoute] Guid clanId,
        [FromServices] IQueryHandler<ClanDetailsDto, GetClanQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetClanQuery(clanId), ct);

    private static async Task<EndpointResult<ClanDto>> Update(
        [FromRoute] Guid clanId,
        [FromBody] UpdateClanRequest request,
        [FromServices] ICommandHandler<ClanDto, UpdateClanCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new UpdateClanCommand(Me(userScope), clanId, request.Name, request.Description), ct);

    private static async Task<EndpointResult<bool>> Disband(
        [FromRoute] Guid clanId,
        [FromServices] ICommandHandler<bool, DisbandClanCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new DisbandClanCommand(Me(userScope), clanId), ct);

    private static async Task<EndpointResult<ClanJoinRequestDto>> RequestJoin(
        [FromRoute] Guid clanId,
        [FromServices] ICommandHandler<ClanJoinRequestDto, RequestJoinCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new RequestJoinCommand(Me(userScope), clanId), ct);

    private static async Task<EndpointResult<IReadOnlyList<ClanJoinRequestDto>>> ListRequests(
        [FromRoute] Guid clanId,
        [FromServices] IQueryHandler<IReadOnlyList<ClanJoinRequestDto>, ListClanRequestsQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new ListClanRequestsQuery(Me(userScope), clanId), ct);

    private static async Task<EndpointResult<ClanJoinRequestDto>> Invite(
        [FromRoute] Guid clanId,
        [FromBody] InvitePlayerRequest request,
        [FromServices] ICommandHandler<ClanJoinRequestDto, InvitePlayerCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new InvitePlayerCommand(Me(userScope), clanId, request.AccountId), ct);

    private static async Task<EndpointResult<ClanMemberDto>> ChangeRole(
        [FromRoute] Guid clanId,
        [FromRoute] Guid accountId,
        [FromBody] ChangeRoleRequest request,
        [FromServices] ICommandHandler<ClanMemberDto, ChangeRoleCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new ChangeRoleCommand(Me(userScope), clanId, accountId, request.Role), ct);

    private static async Task<EndpointResult<bool>> Kick(
        [FromRoute] Guid clanId,
        [FromRoute] Guid accountId,
        [FromServices] ICommandHandler<bool, KickMemberCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new KickMemberCommand(Me(userScope), clanId, accountId), ct);

    private static async Task<EndpointResult<ClanDto>> Transfer(
        [FromRoute] Guid clanId,
        [FromBody] TransferLeadershipRequest request,
        [FromServices] ICommandHandler<ClanDto, TransferLeadershipCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new TransferLeadershipCommand(Me(userScope), clanId, request.AccountId), ct);
}
