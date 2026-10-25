using Shared.SharedKernel.Errors;

namespace MatchmakingService.Domain;

public static class MatchmakingErrors
{
    public static Error PartyNotFound { get; } =
        Error.Validation("matchmaking.party.not_found", "Party was not found", "partyId");

    public static Error PartyCodeNotFound { get; } =
        Error.Validation("matchmaking.party.code.not_found", "Party with this invite code was not found", "code");

    public static Error PartyClosed { get; } =
        Error.Validation("matchmaking.party.closed", "Party is closed", "partyId");

    public static Error PartyFull { get; } =
        Error.Conflict("matchmaking.party.full", "Party is full", "partyId");

    public static Error PartyAlreadyMember { get; } =
        Error.Conflict("matchmaking.party.already_member", "Player is already a party member", "accountId");

    public static Error PartyMemberNotFound { get; } =
        Error.Validation("matchmaking.party.member.not_found", "Party member was not found", "accountId");

    public static Error PartyNotLeader { get; } =
        Error.Validation("matchmaking.party.not_leader", "Only the party leader can perform this action", "accountId");

    public static Error PartySizeInvalid { get; } =
        Error.Validation("matchmaking.party.size.invalid", "Party size must be between 2 and 5", "partySize");

    public static Error PartyNotReady { get; } =
        Error.Validation("matchmaking.party.not_ready", "All party members must be ready", "partyId");

    public static Error PartyAlreadyQueued { get; } =
        Error.Conflict("matchmaking.party.already_queued", "Party is already in the queue", "partyId");

    public static Error PartyInMatch { get; } =
        Error.Conflict("matchmaking.party.in_match", "Party is already in an active match", "partyId");

    public static Error PartyNotQueued { get; } =
        Error.Validation("matchmaking.party.not_queued", "Party is not in the queue", "partyId");

    public static Error PartyNotInMatch { get; } =
        Error.Validation("matchmaking.party.not_in_match", "Party is not in a match", "partyId");

    public static Error QueueTicketNotFound { get; } =
        Error.Validation("matchmaking.queue.not_found", "Queue ticket was not found", "partyId");

    public static Error QueueModeInvalid { get; } =
        Error.Validation("matchmaking.queue.mode.invalid", "Queue mode is required", "mode");

    public static Error MatchNotFound { get; } =
        Error.Validation("matchmaking.match.not_found", "Match was not found", "matchId");

    public static Error MatchNotActive { get; } =
        Error.Validation("matchmaking.match.not_active", "Match is not active", "matchId");

    public static Error MatchAlreadyFinished { get; } =
        Error.Conflict("matchmaking.match.already_finished", "Match is already finished", "matchId");

    public static Error MatchPartiesInvalid { get; } =
        Error.Validation("matchmaking.match.parties.invalid", "Match requires two different queued parties", "parties");

    public static Error MatchDurationInvalid { get; } =
        Error.Validation("matchmaking.match.duration.invalid", "Match duration is invalid", "endsAt");

    public static Error MatchModeInvalid { get; } =
        Error.Validation("matchmaking.match.mode.invalid", "Match mode is invalid", "mode");

    public static Error MatchResultsEmpty { get; } =
        Error.Validation("matchmaking.match.results.empty", "Match results are required", "results");

    public static Error MatchResultAccountInvalid { get; } =
        Error.Validation("matchmaking.match.result.account.invalid", "Result account is not a match participant", "accountId");

    public static Error MatchResultValueInvalid { get; } =
        Error.Validation("matchmaking.match.result.value.invalid", "Match result values must be non-negative", "results");

    public static Error MatchResultDuplicate { get; } =
        Error.Validation("matchmaking.match.result.duplicate", "Match result contains duplicate accounts", "results");

    public static Error MatchParticipantNotFound { get; } =
        Error.Validation("matchmaking.match.participant.not_found", "Player is not a participant of this match", "matchId");

    public static Error RewardInvalid { get; } =
        Error.Validation("matchmaking.reward.invalid", "Match reward is invalid", "reward");

    public static Error RewardCurrencyInvalid { get; } =
        Error.Validation("matchmaking.reward.currency.invalid", "Reward currency is required", "currencyCode");

    public static Error RewardAmountInvalid { get; } =
        Error.Validation("matchmaking.reward.amount.invalid", "Reward amount must be non-negative", "amount");

    public static Error RewardItemsInvalid { get; } =
        Error.Validation("matchmaking.reward.items.invalid", "Reward items are invalid", "items");

    public static Error ExternalMatchInvalid { get; } =
        Error.Validation("matchmaking.match.external.invalid", "External match needs a reference and two non-empty teams of distinct players", "teams");

    public static Error QueueRegionInvalid { get; } =
        Error.Validation("matchmaking.queue.region.invalid", "Region is invalid", "region");

    public static Error MatchAllocationInvalid { get; } =
        Error.Conflict("matchmaking.match.allocation.invalid", "Match is not waiting for a game server", "matchId");

    public static Error GrantNotFound { get; } =
        Error.Validation("matchmaking.reward.grant.not_found", "Match reward grant was not found", "grantId");
}
