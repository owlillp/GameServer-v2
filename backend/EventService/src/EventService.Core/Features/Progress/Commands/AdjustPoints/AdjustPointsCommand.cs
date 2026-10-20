using Shared.Core.Abstractions;

namespace EventService.Core.Features.Progress.Commands.AdjustPoints;

public sealed record AdjustPointsCommand(Guid EventId, Guid AccountId, int Amount) : ICommand;
