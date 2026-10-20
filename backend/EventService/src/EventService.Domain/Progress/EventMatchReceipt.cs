using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Progress;

public sealed class EventMatchReceipt
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid MatchId { get; private set; }

    public DateTime ProcessedAt { get; private set; }

    // EF Core
    private EventMatchReceipt() { }

    private EventMatchReceipt(Guid eventId, Guid accountId, Guid matchId)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        AccountId = accountId;
        MatchId = matchId;
        ProcessedAt = DateTime.UtcNow;
    }

    public static Result<EventMatchReceipt, Error> Create(Guid eventId, Guid accountId, Guid matchId)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        if (matchId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("matchId");
        }

        return new EventMatchReceipt(eventId, accountId, matchId);
    }
}
