using CSharpFunctionalExtensions;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Rewards;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Domain.Matches;

public sealed class Match
{
    private readonly List<MatchParty> _parties = [];
    private readonly List<MatchParticipant> _participants = [];
    private readonly List<MatchResult> _results = [];

    public Guid Id { get; private set; }

    public string Mode { get; private set; } = null!;

    public MatchStatus Status { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public Guid? WinnerPartyId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public MatchRewardDefinition? Reward { get; private set; }

    public IReadOnlyList<MatchParty> Parties => _parties;

    public IReadOnlyList<MatchParticipant> Participants => _participants;

    public IReadOnlyList<MatchResult> Results => _results;

    public bool HasResults => _results.Count > 0;

    // EF Core
    private Match() { }

    public static Result<Match, Error> Create(
        Party partyA,
        Party partyB,
        string mode,
        DateTime startsAt,
        DateTime endsAt,
        MatchRewardData reward)
    {
        if (partyA.Id == partyB.Id || partyA.Status == PartyStatus.Closed || partyB.Status == PartyStatus.Closed)
        {
            return MatchmakingErrors.MatchPartiesInvalid;
        }

        if (partyA.Members.Count < MatchmakingConstants.MIN_PARTY_SIZE
            || partyB.Members.Count < MatchmakingConstants.MIN_PARTY_SIZE)
        {
            return MatchmakingErrors.PartySizeInvalid;
        }

        if (endsAt <= startsAt)
        {
            return MatchmakingErrors.MatchDurationInvalid;
        }

        string normalizedMode = (mode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedMode) || normalizedMode.Length > MatchmakingConstants.MODE_MAX_LENGTH)
        {
            return MatchmakingErrors.MatchModeInvalid;
        }

        var match = new Match
        {
            Id = Guid.CreateVersion7(),
            Mode = normalizedMode,
            Status = MatchStatus.Active,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        match._parties.Add(new MatchParty(match.Id, partyA.Id, 1));
        match._parties.Add(new MatchParty(match.Id, partyB.Id, 2));

        foreach (PartyMember member in partyA.Members)
        {
            match._participants.Add(new MatchParticipant(match.Id, partyA.Id, member.AccountId));
        }

        foreach (PartyMember member in partyB.Members)
        {
            match._participants.Add(new MatchParticipant(match.Id, partyB.Id, member.AccountId));
        }

        var definitionResult = MatchRewardDefinition.Create(match.Id, reward);
        if (definitionResult.IsFailure)
        {
            return definitionResult.Error;
        }

        match.Reward = definitionResult.Value;

        return match;
    }

    public bool IsParticipant(Guid accountId) =>
        _participants.Exists(participant => participant.AccountId == accountId);

    public UnitResult<Error> SubmitResults(IReadOnlyList<MatchResultData> results, DateTime now)
    {
        if (Status != MatchStatus.Active)
        {
            return MatchmakingErrors.MatchNotActive;
        }

        if (results.Count == 0)
        {
            return MatchmakingErrors.MatchResultsEmpty;
        }

        if (results.Count > MatchmakingConstants.MAX_MATCH_RESULTS)
        {
            return MatchmakingErrors.MatchResultsEmpty;
        }

        var seenAccounts = new HashSet<Guid>();

        foreach (MatchResultData result in results)
        {
            if (result.Score < 0 || result.Kills < 0)
            {
                return MatchmakingErrors.MatchResultValueInvalid;
            }

            if (!seenAccounts.Add(result.AccountId))
            {
                return MatchmakingErrors.MatchResultDuplicate;
            }

            MatchParticipant? participant = _participants.Find(
                entry => entry.AccountId == result.AccountId);

            if (participant is null)
            {
                return MatchmakingErrors.MatchResultAccountInvalid;
            }

            MatchResult? existing = _results.Find(entry => entry.AccountId == result.AccountId);
            if (existing is null)
            {
                _results.Add(new MatchResult(
                    Id,
                    result.AccountId,
                    participant.PartyId,
                    result.Score,
                    result.Kills,
                    now));
            }
            else
            {
                existing.Update(result.Score, result.Kills, now);
            }
        }

        UpdatedAt = now;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Finish(DateTime now)
    {
        if (Status == MatchStatus.Finished)
        {
            return MatchmakingErrors.MatchAlreadyFinished;
        }

        if (Status != MatchStatus.Active)
        {
            return MatchmakingErrors.MatchNotActive;
        }

        Guid? winnerPartyId = null;

        if (_parties.Count == 2)
        {
            long firstTotal = PartyTotal(_parties[0].PartyId);
            long secondTotal = PartyTotal(_parties[1].PartyId);

            if (firstTotal > secondTotal)
            {
                winnerPartyId = _parties[0].PartyId;
            }
            else if (secondTotal > firstTotal)
            {
                winnerPartyId = _parties[1].PartyId;
            }
        }

        WinnerPartyId = winnerPartyId;

        foreach (MatchResult result in _results)
        {
            result.SetWin(winnerPartyId is not null && result.PartyId == winnerPartyId);
        }

        Status = MatchStatus.Finished;
        FinishedAt = now;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(DateTime now)
    {
        if (Status == MatchStatus.Finished)
        {
            return MatchmakingErrors.MatchAlreadyFinished;
        }

        if (Status == MatchStatus.Cancelled)
        {
            return MatchmakingErrors.MatchNotActive;
        }

        Status = MatchStatus.Cancelled;
        FinishedAt = now;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public long PartyTotal(Guid partyId) =>
        _results.Where(result => result.PartyId == partyId).Sum(result => result.Score);

    public bool IsExpired(DateTime now) => Status == MatchStatus.Active && EndsAt <= now;

    public bool IsWinner(Guid partyId) => WinnerPartyId == partyId;
}
