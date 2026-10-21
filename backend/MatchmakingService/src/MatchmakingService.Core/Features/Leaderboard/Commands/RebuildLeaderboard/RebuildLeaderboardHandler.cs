using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Leaderboard.Commands.RebuildLeaderboard;

public sealed class RebuildLeaderboardHandler(
    ILeaderboardRepository repository,
    ILeaderboardStore store,
    IOptions<MatchmakingOptions> options) : ICommandHandler<bool, RebuildLeaderboardCommand>
{
    public async Task<Result<bool, Error>> Handle(
        RebuildLeaderboardCommand command,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LeaderboardEntry> entries = await repository.ListAsync(
            options.Value.LeaderboardSize,
            cancellationToken);

        await store.RebuildAsync(entries, cancellationToken);

        return true;
    }
}
