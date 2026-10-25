using MatchmakingService.Core.Abstractions;
using Shared.Core.Database;
using Shared.IntegrationEvents.Matchmaking.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

public sealed class MatchRewardGrantFailedEventHandler(
    IMatchRewardRepository rewardRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(MatchRewardGrantFailedEvent @event, CancellationToken cancellationToken)
    {
        var grant = await rewardRepository.GetAsync(@event.GrantId, cancellationToken);
        if (grant is null)
        {
            return;
        }

        grant.MarkFailed(@event.Reason, DateTime.UtcNow);
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }
}
