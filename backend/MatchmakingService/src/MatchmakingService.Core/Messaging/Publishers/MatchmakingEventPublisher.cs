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

    public Task PublishExternalStartedAsync(Match match, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new ExternalMatchStartedEvent(
            match.ExternalRef!,
            match.Id,
            match.ServerAddress ?? string.Empty,
            match.AllocatedAt ?? DateTime.UtcNow));

    public Task PublishExternalCompletedAsync(Match match, CancellationToken cancellationToken)
    {
        int? winnerSide = match.WinnerPartyId is null ? null : match.SideOf(match.WinnerPartyId.Value);

        ExternalMatchTeamResult[] teams = [.. match.Parties
            .OrderBy(party => party.Side)
            .Select(party => new ExternalMatchTeamResult(
                party.Side,
                match.PartyTotal(party.PartyId),
                [.. match.Participants
                    .Where(participant => participant.PartyId == party.PartyId)
                    .Select(participant =>
                    {
                        MatchResult? result = match.Results.FirstOrDefault(entry => entry.AccountId == participant.AccountId);
                        return new ExternalMatchPlayerResult(participant.AccountId, result?.Score ?? 0, result?.Kills ?? 0);
                    })]))];

        return outbox.PublishAsync(new ExternalMatchCompletedEvent(
            match.ExternalRef!,
            match.Id,
            match.Mode,
            winnerSide,
            teams,
            match.FinishedAt ?? DateTime.UtcNow));
    }

    public Task PublishExternalCancelledAsync(Match match, string reason, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new ExternalMatchCancelledEvent(
            match.ExternalRef!,
            match.Id,
            reason,
            match.FinishedAt ?? DateTime.UtcNow));
}
