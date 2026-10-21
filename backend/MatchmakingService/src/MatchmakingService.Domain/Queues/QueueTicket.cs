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

    public QueueTicketStatus Status { get; private set; }

    public Guid? MatchId { get; private set; }

    public DateTime EnqueuedAt { get; private set; }

    public DateTime? MatchedAt { get; private set; }

    // EF Core
    private QueueTicket() { }

    private QueueTicket(Party party, string mode, DateTime now)
    {
        Id = Guid.CreateVersion7();
        PartyId = party.Id;
        LeaderAccountId = party.LeaderAccountId;
        PartySize = party.Members.Count;
        Mode = mode;
        Status = QueueTicketStatus.Queued;
        EnqueuedAt = now;
    }

    public static Result<QueueTicket, Error> Create(Party party, string mode, DateTime now)
    {
        if (party.Status != PartyStatus.Queued)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            return MatchmakingErrors.QueueModeInvalid;
        }

        return new QueueTicket(party, mode.Trim().ToUpperInvariant(), now);
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
