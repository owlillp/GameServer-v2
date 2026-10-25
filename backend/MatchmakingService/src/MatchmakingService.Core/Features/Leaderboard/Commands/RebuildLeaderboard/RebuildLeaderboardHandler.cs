using CSharpFunctionalExtensions;
using MatchmakingService.Core.Services;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Leaderboard.Commands.RebuildLeaderboard;

public sealed class RebuildLeaderboardHandler(LeaderboardSync sync) : ICommandHandler<bool, RebuildLeaderboardCommand>
{
    /// <summary>
    /// Returns false when another replica is rebuilding right now.
    /// </summary>
    public async Task<Result<bool, Error>> Handle(
        RebuildLeaderboardCommand command,
        CancellationToken cancellationToken = default)
    {
        LeaderboardSyncOutcome outcome = await sync.SyncAsync(force: true, cancellationToken);

        return outcome == LeaderboardSyncOutcome.Rebuilt;
    }
}
