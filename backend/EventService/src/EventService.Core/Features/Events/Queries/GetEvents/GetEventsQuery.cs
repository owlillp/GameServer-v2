using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Queries.GetEvents;

public sealed record GetEventsQuery(bool IncludeUnpublished = false, string? Phase = null) : IQuery;
