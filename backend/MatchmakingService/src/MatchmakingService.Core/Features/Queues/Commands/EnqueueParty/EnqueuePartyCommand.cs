using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Queues.Commands.EnqueueParty;

public sealed record EnqueuePartyCommand(Guid AccountId, string? Mode, string? Region = null) : ICommand;
