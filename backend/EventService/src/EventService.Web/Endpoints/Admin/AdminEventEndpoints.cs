using EventService.Contracts.Dtos;
using EventService.Contracts.Requests;
using EventService.Core.Features.Events.Commands.ArchiveEvent;
using EventService.Core.Features.Events.Commands.CreateEvent;
using EventService.Core.Features.Events.Commands.PublishEvent;
using EventService.Core.Features.Events.Commands.UpdateEvent;
using EventService.Core.Features.Events.Queries.GetEvent;
using EventService.Core.Features.Events.Queries.GetEvents;
using EventService.Core.Features.Progress.Commands.AdjustPoints;
using EventService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace EventService.Web.Endpoints.Admin;

public sealed class AdminEventEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/events";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Events")
            .RequireAuthorization();

        group.MapGet("", GetEvents)
            .WithName("Admin.Events.List")
            .WithSummary("List all events including drafts and archived")
            .RequirePermissions(EventPermissions.Events.VIEW);

        group.MapGet("/{eventId:guid}", GetEvent)
            .WithName("Admin.Events.Get")
            .WithSummary("Get event by id")
            .RequirePermissions(EventPermissions.Events.VIEW);

        group.MapPost("", CreateEvent)
            .WithName("Admin.Events.Create")
            .WithSummary("Create a draft event")
            .RequirePermissions(EventPermissions.Events.MANAGE);

        group.MapPut("/{eventId:guid}", UpdateEvent)
            .WithName("Admin.Events.Update")
            .WithSummary("Update a draft event")
            .RequirePermissions(EventPermissions.Events.MANAGE);

        group.MapPost("/{eventId:guid}/publish", PublishEvent)
            .WithName("Admin.Events.Publish")
            .WithSummary("Publish an event")
            .RequirePermissions(EventPermissions.Events.MANAGE);

        group.MapDelete("/{eventId:guid}", ArchiveEvent)
            .WithName("Admin.Events.Archive")
            .WithSummary("Archive an event")
            .RequirePermissions(EventPermissions.Events.MANAGE);

        group.MapPost("/{eventId:guid}/accounts/{accountId:guid}/points", AdjustPoints)
            .WithName("Admin.Events.Points.Adjust")
            .WithSummary("Adjust event points for an account")
            .RequirePermissions(EventPermissions.Events.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<EventDto>>> GetEvents(
        [FromQuery] string? phase,
        [FromServices] IQueryHandler<IReadOnlyList<EventDto>, GetEventsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetEventsQuery(IncludeUnpublished: true, phase), ct);

    private static async Task<EndpointResult<EventDto>> GetEvent(
        [FromRoute] Guid eventId,
        [FromServices] IQueryHandler<EventDto, GetEventQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetEventQuery(eventId), ct);

    private static async Task<EndpointResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        [FromServices] ICommandHandler<EventDto, CreateEventCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateEventCommand(request), ct);

    private static async Task<EndpointResult<EventDto>> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] UpdateEventRequest request,
        [FromServices] ICommandHandler<EventDto, UpdateEventCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateEventCommand(eventId, request), ct);

    private static async Task<EndpointResult<EventDto>> PublishEvent(
        [FromRoute] Guid eventId,
        [FromServices] ICommandHandler<EventDto, PublishEventCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new PublishEventCommand(eventId), ct);

    private static async Task<EndpointResult<EventDto>> ArchiveEvent(
        [FromRoute] Guid eventId,
        [FromServices] ICommandHandler<EventDto, ArchiveEventCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new ArchiveEventCommand(eventId), ct);

    private static async Task<EndpointResult<EventProgressDto>> AdjustPoints(
        [FromRoute] Guid eventId,
        [FromRoute] Guid accountId,
        [FromBody] AdjustEventPointsRequest request,
        [FromServices] ICommandHandler<EventProgressDto, AdjustPointsCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new AdjustPointsCommand(eventId, accountId, request.Amount), ct);
}
