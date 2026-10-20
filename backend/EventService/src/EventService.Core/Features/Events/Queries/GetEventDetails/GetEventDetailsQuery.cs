using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Queries.GetEventDetails;

public sealed record GetEventDetailsQuery(string EventCode, Guid AccountId) : IQuery;
