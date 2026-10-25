using MatchmakingService.Core.Abstractions;
using Shared.Core.Database;
using Shared.IntegrationEvents.Matchmaking.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

public sealed class MatchRewardGrantedEventHandler(
    IMatchRewardRepository rewardRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(MatchRewardGrantedEvent @event, CancellationToken cancellationToken)
    {
        var grant = await rewardRepository.GetAsync(@event.GrantId, cancellationToken);
        if (grant is null)
        {
            return;
        }

        grant.MarkItemsGranted(DateTime.UtcNow);
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }
}
