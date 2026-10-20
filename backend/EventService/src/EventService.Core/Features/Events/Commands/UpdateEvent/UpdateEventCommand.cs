using EventService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(Guid EventId, UpdateEventRequest Request) : ICommand;
