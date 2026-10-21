using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Commands.FinishMatch;

public sealed record FinishMatchCommand(Guid MatchId) : ICommand;
