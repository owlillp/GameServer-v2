using EventService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(CreateEventRequest Request) : ICommand;
