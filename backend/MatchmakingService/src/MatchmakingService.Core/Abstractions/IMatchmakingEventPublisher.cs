using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.Core.Abstractions;

public interface IMatchmakingEventPublisher
{
    Task PublishRewardGrantAsync(MatchRewardGrant grant, CancellationToken cancellationToken);

    Task PublishMatchCompletedAsync(Match match, MatchResult result, CancellationToken cancellationToken);

    Task PublishAllocateMatchAsync(Match match, CancellationToken cancellationToken);

    Task PublishMatchReadyAsync(Match match, CancellationToken cancellationToken);
}
