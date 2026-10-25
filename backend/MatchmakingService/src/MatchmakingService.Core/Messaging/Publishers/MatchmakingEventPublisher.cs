using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.IntegrationEvents.Matchmaking.Events;
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

    public Task PublishAllocateMatchAsync(Match match, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new AllocateMatchCommand(
            match.Id,
            match.Mode,
            match.Region,
            [.. match.Participants.Select(participant => new AllocateMatchParticipant(
                participant.AccountId,
                participant.PartyId,
                match.Parties.First(party => party.PartyId == participant.PartyId).Side))],
            match.AllocationRequestedAt ?? DateTime.UtcNow));

    public Task PublishMatchReadyAsync(Match match, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new MatchReadyEvent(
            match.Id,
            match.Mode,
            match.ServerAddress ?? string.Empty,
            [.. match.Participants.Select(participant => participant.AccountId)],
            match.AllocatedAt ?? DateTime.UtcNow));
}
