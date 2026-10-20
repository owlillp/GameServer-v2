using EventService.Contracts.Dtos;
using EventService.Core.Features.Events.Queries.GetEventDetails;
using EventService.Core.Features.Events.Queries.GetEvents;
using EventService.Core.Features.Progress.Commands.ClaimReward;
using EventService.Core.Features.Progress.Queries.GetMyClaims;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace EventService.Web.Endpoints.Events;

public sealed class EventEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/events";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Events")
            .RequireAuthorization();

        group.MapGet("", GetEvents)
            .WithName("Events.List")
            .WithSummary("List published events");

        group.MapGet("/active", GetActiveEvents)
            .WithName("Events.Active")
            .WithSummary("List currently active events");

        group.MapGet("/me/claims", GetMyClaims)
            .WithName("Events.Claims.List")
            .WithSummary("List current player reward claims");

        group.MapGet("/{eventCode}", GetEvent)
            .WithName("Events.Get")
            .WithSummary("Get event details with the current player progress");

        group.MapPost("/{eventCode}/rewards/{rewardCode}/claim", ClaimReward)
            .WithName("Events.Rewards.Claim")
            .WithSummary("Claim an event reward");

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<EventDto>>> GetEvents(
        [FromQuery] string? phase,
        [FromServices] IQueryHandler<IReadOnlyList<EventDto>, GetEventsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetEventsQuery(IncludeUnpublished: false, phase), ct);

    private static async Task<EndpointResult<IReadOnlyList<EventDto>>> GetActiveEvents(
        [FromServices] IQueryHandler<IReadOnlyList<EventDto>, GetEventsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetEventsQuery(IncludeUnpublished: false, Phase: "ACTIVE"), ct);

    private static async Task<EndpointResult<EventDetailsDto>> GetEvent(
        [FromRoute] string eventCode,
        [FromServices] IQueryHandler<EventDetailsDto, GetEventDetailsQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetEventDetailsQuery(eventCode, userScope.Id ?? Guid.Empty), ct);

    private static async Task<EndpointResult<EventRewardClaimDto>> ClaimReward(
        [FromRoute] string eventCode,
        [FromRoute] string rewardCode,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICommandHandler<EventRewardClaimDto, ClaimRewardCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(
            new ClaimRewardCommand(eventCode, rewardCode, userScope.Id ?? Guid.Empty, idempotencyKey),
            ct);

    private static async Task<EndpointResult<IReadOnlyList<EventRewardClaimDto>>> GetMyClaims(
        [FromQuery] Guid? eventId,
        [FromServices] IQueryHandler<IReadOnlyList<EventRewardClaimDto>, GetMyClaimsQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyClaimsQuery(userScope.Id ?? Guid.Empty, eventId), ct);
}
