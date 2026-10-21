using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Commands.SetReady;

public sealed record SetReadyCommand(Guid AccountId, bool IsReady) : ICommand;
