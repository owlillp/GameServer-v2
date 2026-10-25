using CSharpFunctionalExtensions;
using MatchmakingService.Domain.Parties;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Domain.Queues;

public sealed class QueueTicket
{
    public Guid Id { get; private set; }

    public Guid PartyId { get; private set; }

    public Guid LeaderAccountId { get; private set; }

    public int PartySize { get; private set; }

    public string Mode { get; private set; } = null!;

    public string Region { get; private set; } = MatchmakingConstants.DEFAULT_REGION;

    /// <summary>
    /// Re-queued after a failed game server allocation: paired before regular tickets.
    /// </summary>
    public bool IsPriority { get; private set; }

    /// <summary>
    /// How many times the party has been put back after a failed allocation.
    /// </summary>
    public int Attempt { get; private set; }

    public QueueTicketStatus Status { get; private set; }

    public Guid? MatchId { get; private set; }

    public DateTime EnqueuedAt { get; private set; }

    public DateTime? MatchedAt { get; private set; }

    // EF Core
    private QueueTicket() { }

    private QueueTicket(Party party, string mode, string region, DateTime now)
    {
        Region = region;
        Id = Guid.CreateVersion7();
        PartyId = party.Id;
        LeaderAccountId = party.LeaderAccountId;
        PartySize = party.Members.Count;
        Mode = mode;
        Status = QueueTicketStatus.Queued;
        EnqueuedAt = now;
    }

    public static Result<QueueTicket, Error> Create(Party party, string mode, DateTime now) =>
        Create(party, mode, MatchmakingConstants.DEFAULT_REGION, now);

    public static Result<QueueTicket, Error> Create(Party party, string mode, string region, DateTime now)
    {
        string normalizedRegion = (region ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedRegion.Length is 0 or > MatchmakingConstants.REGION_MAX_LENGTH)
        {
            return MatchmakingErrors.QueueRegionInvalid;
        }

        if (party.Status != PartyStatus.Queued)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            return MatchmakingErrors.QueueModeInvalid;
        }

        return new QueueTicket(party, mode.Trim().ToUpperInvariant(), normalizedRegion, now);
    }

    /// <summary>
    /// Ticket for a party whose match was cancelled because no game server took it.
    /// Keeps the original queue time so the party does not lose its place.
    /// </summary>
    public Result<QueueTicket, Error> Requeue(Party party, DateTime now)
    {
        var result = Create(party, Mode, Region, now);
        if (result.IsFailure)
        {
            return result.Error;
        }

        QueueTicket ticket = result.Value;
        ticket.EnqueuedAt = EnqueuedAt;
        ticket.IsPriority = true;
        ticket.Attempt = Attempt + 1;

        return ticket;
    }

    public UnitResult<Error> Match(Guid matchId, DateTime now)
    {
        if (Status != QueueTicketStatus.Queued)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        Status = QueueTicketStatus.Matched;
        MatchId = matchId;
        MatchedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(DateTime now)
    {
        if (Status != QueueTicketStatus.Queued)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        Status = QueueTicketStatus.Cancelled;
        MatchedAt = now;

        return UnitResult.Success<Error>();
    }
}
