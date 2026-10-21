using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Queues.Commands.LeaveQueue;

public sealed record LeaveQueueCommand(Guid AccountId) : ICommand;
