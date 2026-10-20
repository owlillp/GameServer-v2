using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Events.Commands.ArchiveEvent;

public sealed record ArchiveEventCommand(Guid EventId) : ICommand;
