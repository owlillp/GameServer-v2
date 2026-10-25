using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Domain.Leaderboard;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.Core.Features.Matchmaking.Models;

internal static class MatchmakingMapper
{
    public static PartyMemberDto ToDto(this PartyMember member) =>
        new(member.AccountId, member.IsReady, member.JoinedAt);

    public static PartyDto ToDto(this Party party) =>
        new(
            party.Id,
            party.Code,
            party.LeaderAccountId,
            party.Status.ToString().ToUpperInvariant(),
            party.Members.Count,
            party.CreatedAt,
            [.. party.Members
                .OrderBy(member => member.JoinedAt)
                .Select(member => member.ToDto())]);

    public static QueueTicketDto ToDto(this QueueTicket ticket) =>
        new(
            ticket.Id,
            ticket.PartyId,
            ticket.Mode,
            ticket.Status.ToString().ToUpperInvariant(),
            ticket.PartySize,
            ticket.EnqueuedAt,
            ticket.MatchId,
            ticket.Region);

    public static MatchRewardDto ToDto(this MatchRewardDefinition reward) =>
        new(
            reward.CurrencyCode,
            reward.ParticipationAmount,
            reward.WinBonusAmount,
            [.. reward.Items.Select(item => new MatchRewardItemDto(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))]);

    public static MatchRewardGrantDto ToDto(this MatchRewardGrant grant) =>
        new(
            grant.Id,
            grant.AccountId,
            grant.Amount,
            grant.Status.ToString().ToUpperInvariant(),
            grant.FailureReason,
            grant.WalletCreditedAt,
            grant.ItemsGrantedAt);

    public static LeaderboardEntryDto ToDto(this LeaderboardEntry entry, int rank) =>
        new(rank, entry.AccountId, entry.Points, entry.Wins, entry.MatchesPlayed);

    public static MatchSummaryDto ToSummaryDto(this Match match) =>
        new(
            match.Id,
            match.Mode,
            match.Status.ToString().ToUpperInvariant(),
            match.StartsAt,
            match.EndsAt,
            match.FinishedAt,
            match.WinnerPartyId);

    public static MatchDetailsDto ToDetailsDto(this Match match, Guid? accountId = null) =>
        new(
            match.Id,
            match.Mode,
            match.Status.ToString().ToUpperInvariant(),
            match.StartsAt,
            match.EndsAt,
            match.FinishedAt,
            match.WinnerPartyId,
            accountId is null
                ? null
                : match.Participants
                    .FirstOrDefault(participant => participant.AccountId == accountId)
                    ?.PartyId,
            [.. match.Parties
                .OrderBy(party => party.Side)
                .Select(party => new MatchPartyDto(party.PartyId, party.Side, match.PartyTotal(party.PartyId)))],
            [.. match.Participants.Select(participant => new MatchParticipantDto(participant.AccountId, participant.PartyId))],
            [.. match.Results
                .OrderByDescending(result => result.Score)
                .Select(result => new MatchResultDto(
                    result.AccountId,
                    result.PartyId,
                    result.Score,
                    result.Kills,
                    result.IsWin,
                    result.UpdatedAt))],
            match.Reward is null ? null : match.Reward.ToDto(),
            match.Region);

    public static AdminMatchDto ToAdminDto(this Match match, IReadOnlyList<MatchRewardGrant> grants) =>
        new(match.ToDetailsDto(), [.. grants.Select(grant => grant.ToDto())]);
}
