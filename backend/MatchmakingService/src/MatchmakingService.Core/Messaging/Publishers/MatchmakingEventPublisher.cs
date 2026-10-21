using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.IntegrationEvents.Sessions.Events;

namespace MatchmakingService.Core.Messaging.Publishers;

public sealed class MatchmakingEventPublisher(IOutboxService outbox) : IMatchmakingEventPublisher
{
    public Task PublishRewardGrantAsync(MatchRewardGrant grant, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new GrantMatchRewardCommand(
            grant.Id,
            grant.MatchId,
            grant.AccountId,
            [.. grant.Items.Select(item => new GrantMatchRewardItem(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            DateTime.UtcNow));

    public Task PublishMatchCompletedAsync(
        Match match,
        MatchResult result,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new MatchCompletedEvent(
            match.Id,
            result.AccountId,
            match.Mode,
            result.IsWin,
            result.Kills,
            (int)Math.Clamp(result.Score, 0, int.MaxValue),
            DateTime.UtcNow));
}
