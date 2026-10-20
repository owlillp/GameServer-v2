using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Commands.PublishEvent;

public sealed record PublishEventCommand(Guid EventId) : ICommand;
