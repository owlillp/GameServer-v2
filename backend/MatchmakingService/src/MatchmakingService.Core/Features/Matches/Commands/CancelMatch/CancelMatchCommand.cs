using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Commands.CancelMatch;

public sealed record CancelMatchCommand(Guid MatchId) : ICommand;
