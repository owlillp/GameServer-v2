using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SessionService.Contracts.Dtos;
using SessionService.Contracts.Requests;
using SessionService.Core.Features.Sessions.Commands.JoinFriend;
using SessionService.Core.Features.Sessions.Commands.JoinSession;
using SessionService.Core.Features.Sessions.Queries.GetMySession;
using SessionService.Core.Services;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Errors;

namespace SessionService.Web.Endpoints.Sessions;

public sealed class SessionEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/sessions";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Sessions")
            .RequireAuthorization();

        group.MapPost("/join", Join)
            .WithName("Sessions.Join")
            .WithSummary("Enter the open world: returns the server address and a join ticket");

        group.MapPost("/join-friend/{accountId:guid}", JoinFriend)
            .WithName("Sessions.JoinFriend")
            .WithSummary("Join the open-world session a friend is in");

        group.MapGet("/me", GetMySession)
            .WithName("Sessions.Me")
            .WithSummary("Get the session the current player is in");

        group.MapGet("/ticket-key", GetTicketKey)
            .WithName("Sessions.TicketKey")
            .WithSummary("Public key for offline join ticket checks")
            .AllowAnonymous();

        return group;
    }

    private static async Task<EndpointResult<SessionTicketDto>> Join(
        [FromBody] JoinSessionRequest request,
        [FromServices] ICommandHandler<SessionTicketDto, JoinSessionCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new JoinSessionCommand(userScope.Id ?? Guid.Empty, request.Region, request.Mode), ct);

    private static async Task<EndpointResult<SessionTicketDto>> JoinFriend(
        [FromRoute] Guid accountId,
        [FromServices] ICommandHandler<SessionTicketDto, JoinFriendCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new JoinFriendCommand(userScope.Id ?? Guid.Empty, accountId), ct);

    private static async Task<EndpointResult<SessionDto>> GetMySession(
        [FromServices] IQueryHandler<SessionDto, GetMySessionQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMySessionQuery(userScope.Id ?? Guid.Empty), ct);

    private static EndpointResult<TicketKeyDto> GetTicketKey([FromServices] JoinTicketService tickets) =>
        Result.Success<TicketKeyDto, Error>(new TicketKeyDto(tickets.KeyId, SecurityAlgorithms.RsaSha256, tickets.PublicKeyPem));
}
