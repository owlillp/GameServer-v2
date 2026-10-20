using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Queries.GetEvent;

public sealed record GetEventQuery(Guid EventId) : IQuery;
